-- Enable Read Committed Snapshot Isolation (RCSI) - allows GET requests to read data without being blocked by INSERT batches
PRINT 'Enabling READ_COMMITTED_SNAPSHOT on [OT-Assessment-DB]...';
ALTER DATABASE [OT-Assessment-DB] SET READ_COMMITTED_SNAPSHOT ON;
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
        WagerId UNIQUEIDENTIFIER PRIMARY KEY,
        Theme NVARCHAR(256),
        Provider NVARCHAR(256),
        GameName NVARCHAR(256),
        TransactionId NVARCHAR(256),
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
        TransactionId NVARCHAR(256),
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
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

        -- Update the Leaderboard data
        -- Using MERGE to maintain idempotency and handle both new and existing accounts in a single statement
        MERGE PlayerSpendStats AS target
        USING (
             -- Using MAX(Username) to satisfy GROUP BY and ensure the MERGE source contains exactly one row per AccountId (accounting for possible username changes)
            SELECT AccountId, MAX(Username) as Username, SUM(Amount) as BatchTotal 
            FROM @Wagers 
            GROUP BY AccountId
        ) AS source
        ON (target.AccountId = source.AccountId)
        WHEN MATCHED THEN
            UPDATE SET 
                target.TotalSpend += source.BatchTotal,
                target.LastUpdated = GETUTCDATE()
        WHEN NOT MATCHED THEN
            INSERT (AccountId, Username, TotalSpend, LastUpdated)
            VALUES (source.AccountId, source.Username, source.BatchTotal, GETUTCDATE());

        -- Bulk Insert the Wagers
        -- Note: We check for existence to preventing duplicates - idempotency
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
        FROM @Wagers src
        WHERE NOT EXISTS (SELECT 1 FROM Wagers w WHERE w.WagerId = src.WagerId);

    COMMIT TRANSACTION;
END;
GO
PRINT 'Database schema generation script completed successfully!';