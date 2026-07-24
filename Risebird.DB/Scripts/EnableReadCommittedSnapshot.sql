-- OPTIONAL ? coordinate with DBA before running on any shared database.
-- Enables READ_COMMITTED_SNAPSHOT so readers see last-committed versions
-- instead of waiting on writer locks during RiskCategory / TrialFlags backfills.
--
-- Prerequisites: verify tempdb headroom; test whole portal on staging first.
-- See Docs/trial-flag-review-read-architecture.md Part 2.

/*
ALTER DATABASE [YOUR_DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
ALTER DATABASE [YOUR_DB_NAME] SET READ_COMMITTED_SNAPSHOT ON;
ALTER DATABASE [YOUR_DB_NAME] SET MULTI_USER;
*/

-- Verify:
-- SELECT name, is_read_committed_snapshot_on FROM sys.databases WHERE name = DB_NAME();
