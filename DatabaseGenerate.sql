-- Enable Read Committed Snapshot Isolation (RCSI) - allows GET requests to read data without being blocked by INSERT batches
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'OT-Assessment-DB' AND is_read_committed_snapshot_on = 1)
BEGIN
    PRINT 'Enabling READ_COMMITTED_SNAPSHOT on [OT-Assessment-DB]...';
    -- ROLLBACK IMMEDIATE ensures that if there are any active transactions that would be affected by this change, 
    --  they are rolled back immediately to allow the setting to be applied without waiting
    ALTER DATABASE [OT-Assessment-DB] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE; 
END
GO

PRINT 'Checking / Creating PlayerSpendStats table...';
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlayerSpendStats')
BEGIN
    CREATE TABLE PlayerSpendStats (
        AccountId UNIQUEIDENTIFIER PRIMARY KEY,
        Username NVARCHAR(256) NOT NULL,
        TotalSpend DECIMAL(18, 2) DEFAULT 0,
        LastUpdated DATETIME2 DEFAULT GETUTCDATE()
    );
END

PRINT 'Checking / Creating Wagers table...';
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Wagers')
BEGIN
    CREATE TABLE Wagers (
        WagerId UNIQUEIDENTIFIER PRIMARY KEY NONCLUSTERED, -- Primary Key NONCLUSTERED to prevent fragmentation from random GUIDs
        Theme NVARCHAR(256),
        Provider NVARCHAR(256),
        GameName NVARCHAR(256),
        TransactionId UNIQUEIDENTIFIER,
        BrandId UNIQUEIDENTIFIER,
        AccountId UNIQUEIDENTIFIER NOT NULL,
        Username NVARCHAR(256),
        ExternalReferenceId UNIQUEIDENTIFIER,
        TransactionTypeId UNIQUEIDENTIFIER,
        Amount DECIMAL(18, 2) NOT NULL,
        CreatedDateTime DATETIME2 NOT NULL,
        NumberOfBets INT,
        CountryCode NVARCHAR(10),
        SessionData NVARCHAR(MAX),
        Duration BIGINT
    );

    -- Clustered Index on CreatedDateTime to keep data physically ordered by time
    CREATE CLUSTERED INDEX IX_Wagers_CreatedDateTime 
    ON Wagers (CreatedDateTime);

    -- Composite Index for Pagination
    CREATE INDEX IX_Wagers_AccountId_CreatedDate 
    ON Wagers (AccountId, CreatedDateTime DESC);
END
GO

-- BATCHING: Table-Valued Parameter (TVP) Type - allows Consumer to send 100s of records in a single call
PRINT 'Checking / Creating WagerTableType (TVP)...';
IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'WagerTableType')
BEGIN
    CREATE TYPE WagerTableType AS TABLE (
        WagerId UNIQUEIDENTIFIER,
        Theme NVARCHAR(256),
        Provider NVARCHAR(256),
        GameName NVARCHAR(256),
        TransactionId UNIQUEIDENTIFIER,
        BrandId UNIQUEIDENTIFIER,
        AccountId UNIQUEIDENTIFIER,
        Username NVARCHAR(256),
        ExternalReferenceId UNIQUEIDENTIFIER,
        TransactionTypeId UNIQUEIDENTIFIER,
        Amount DECIMAL(18, 2),
        CreatedDateTime DATETIME2,
        NumberOfBets INT,
        CountryCode NVARCHAR(10),
        SessionData NVARCHAR(MAX),
        Duration BIGINT
    );
END
GO

-- Bulk Insert Stored Procedure
PRINT 'Creating / Altering sp_ProcessWagerBatch stored procedure...';
GO
CREATE OR ALTER PROCEDURE sp_ProcessWagerBatch
    @Wagers WagerTableType READONLY
AS
BEGIN
    SET NOCOUNT ON; -- Prevents extra result sets from interfering with output
    SET XACT_ABORT ON; -- Ensures that if any statement within the transaction fails, the entire transaction is rolled back

    -- Deduplicate the incoming batch itself - ensures we only take the latest one if the same ID appears twice in @Wagers
    -- NOTE: testing shows duplicates are being sent in the same batch, so we handle this at the start of the procedure to avoid issues with the rest of the logic
    SELECT * INTO #CleanBatch
    FROM (
        SELECT *,
               ROW_NUMBER() OVER (PARTITION BY WagerId ORDER BY CreatedDateTime DESC) as RowNum
        FROM @Wagers
    ) AS src
    WHERE RowNum = 1;

    BEGIN TRANSACTION;
        
        -- Bulk Insert the Wagers
        -- NOTE: We check for existence to preventing duplicates - idempotency
        -- RabbitMQ may retry the same batch or in a scaled environment the same batch may be processed by multiple instances, so we ensure that we do not insert duplicate WagerIds
        INSERT INTO Wagers (
            WagerId, Theme, Provider, GameName, TransactionId, BrandId, 
            AccountId, Username, ExternalReferenceId, TransactionTypeId, 
            Amount, CreatedDateTime, NumberOfBets, CountryCode, SessionData, Duration
        )
        SELECT 
            src.WagerId, src.Theme, src.Provider, src.GameName, src.TransactionId, src.BrandId,
            src.AccountId, src.Username, src.ExternalReferenceId, src.TransactionTypeId,
            src.Amount, src.CreatedDateTime, src.NumberOfBets, src.CountryCode, src.SessionData, src.Duration
        FROM #CleanBatch src
        WHERE NOT EXISTS (SELECT 1 FROM Wagers w WHERE w.WagerId = src.WagerId);

        -- Update existing accounts
        UPDATE target
        SET target.TotalSpend += source.BatchTotal,
            target.LastUpdated = GETUTCDATE()
        FROM PlayerSpendStats AS target
        INNER JOIN (
            SELECT AccountId, SUM(Amount) as BatchTotal 
            FROM #CleanBatch
            GROUP BY AccountId
        ) AS source ON target.AccountId = source.AccountId;

        -- Insert new accounts
        INSERT INTO PlayerSpendStats (AccountId, Username, TotalSpend, LastUpdated)
        SELECT AccountId, MAX(Username), SUM(Amount), GETUTCDATE()
        FROM #CleanBatch AS source
        WHERE NOT EXISTS (SELECT 1 FROM PlayerSpendStats target WHERE target.AccountId = source.AccountId)
        GROUP BY AccountId;

    COMMIT TRANSACTION;

    -- Explicitly drop temp table to help tempdb reclaim resources
    DROP TABLE #CleanBatch;
END;
GO
PRINT 'Database schema generation script completed successfully!';