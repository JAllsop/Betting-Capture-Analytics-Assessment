# Architectural Notes & Design Decisions

This document outlines the trade-offs, design choices, and optimizations made to ensure the system could comfortably handle the 500 RPS benchmark without data loss or blocking

## 1. Database Design & Optimization

The database was thoroughly optimized for **throughput and fast reads** over normalization or standard relational database mapping

- **No Separate `Player` Table:** In a standard relational model, a `Player` table would hold the `Username`, and the `Wagers` table would hold a Foreign Key. To avoid the overhead of referential integrity checks during bulk inserts, the schema is denormalized - `Username` is stored directly on the `Wager` and updated in `PlayerSpendStats` via an upsert
- **Nonclustered Primary Key:** Random `UNIQUEIDENTIFIER` (GUID) values cause index fragmentation (page splitting) when used as a Clustered Primary Key. The PK on `Wagers` was set to `NONCLUSTERED`
- **Time Clustering Index:** A `CLUSTERED INDEX` was placed on `CreatedDateTime` - ensuring that rows are written to disk sequentially,improving insert speeds and pagination performance
- **Pagination Composite Index:** An index on `(AccountId, CreatedDateTime DESC)` was added. Combined with Dapper's `OFFSET/FETCH` multi-queries, this allows the database to locate a player's history without table scans

## 2. The `sp_ProcessWagerBatch` Stored Procedure

Rather than relying on an ORM like Entity Framework for inserts, Dapper and a **Table-Valued Parameter (TVP)** were used

- MassTransit buffers 200 wagers in memory, and the TVP sends them to the SQL DB in a single network round-trip
- **Idempotency:** The Stored Procedure handles deduplication using `ROW_NUMBER()` to clean the incoming batch, followed by a `NOT EXISTS` during the insert - ensuring that if RabbitMQ delivers the same message twice (at-least-once delivery), the player's `TotalSpend` isn't incorrectly increased

## 3. Caching & High-Performance Reads

To ensure the `GET` endpoints survive the load test, the read-path was decoupled from the SQL DB ingestion

- **Redis Sorted Sets:** The Top Spenders leaderboard is stored in a Redis Sorted Set (`ZREVRANGE`) - shifts the sorting burden off SQL Server
- **Cache Warmer & Polly:** A background `IHostedService` runs on startup to hydrate Redis from SQL. Because Docker containers start simultaneously, **Polly** was used to apply an exponential backoff retry policy, preventing the service from crashing before SQL/Redis were healthy

## 4. Modifications to the `BogusGenerator` Tester

To prove the system has no data loss, the tester application required slight modifications

- **Audit Persistence:** The existing classes were untouched, but a `ConcurrentBag` was added to capture every generated wager
- **Reconciliation Output:** After the NBomber run finishes, the tester saves the payloads to `sent_wagers_audit.json` in the `data_audit` dir. The API has a `/debug/testResults` endpoint to compare this JSON with what was received by the controller

## 5. Security & Deployment Concessions

- **Swagger & Error Details:** Left enabled by default globally for the reviewer
- **Passwords:** Connection strings and passwords (`Guest123!`) are hardcoded in the Aspire AppHost and Docker configs to enable easy setup and use by the reviewer
