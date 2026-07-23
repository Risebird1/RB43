/* =============================================================================
   Trial Flag Review — Part 0 diagnosis (lazy two-tier reads)
   Run on staging/prod READ-ONLY before/after deploy of:
     Risebird.DB/Indexes/TrialFlagReviewReadIndexes.sql
     usp_GetTrialFlagsForReview_List / usp_GetTrialFlagDetail

   Capture results in the PR (cold + during RiskCategory/TrialFlags backfill).
   ============================================================================= */

-- 1) READ_COMMITTED_SNAPSHOT status (DBA decision; do not enable from app deploy)
SELECT
    DB_NAME() AS DbName,
    is_read_committed_snapshot_on AS ReadCommittedSnapshotOn,
    snapshot_isolation_state_desc AS SnapshotIsolation
FROM sys.databases
WHERE name = DB_NAME();

-- 2) Probe indexes present?
SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc
FROM sys.indexes i
WHERE i.name IN (
    N'IX_PaymentSLADetails_InterviewId',
    N'IX_QcInterviewAIevaluation_InterviewId',
    N'IX_CandidateFeedbackDetails_InterviewId',
    N'IX_RedoInterviews_InterviewId',
    N'IX_InterviewMeetingsJoinTime_InterviewId',
    N'IX_RiskCategory_InterviewId',
    N'IX_InterviewComments_InterviewId'
)
ORDER BY TableName, IndexName;

-- 3) Queue size + severity mix (list pressure)
SELECT ReviewStatus, COUNT(*) AS Cnt
FROM dbo.TrialFlags WITH (READCOMMITTED)
GROUP BY ReviewStatus;

SELECT Severity, COUNT(*) AS Cnt
FROM dbo.TrialFlags WITH (READCOMMITTED)
GROUP BY Severity
ORDER BY Severity;

-- 4) Smoke-time List (page 1 pending) — SET STATISTICS TIME/IO ON in SSMS first
/*
SET STATISTICS TIME ON; SET STATISTICS IO ON;
EXEC dbo.usp_GetTrialFlagsForReview_List
    @ReviewStatus = 'pending', @Offset = 0, @PageSize = 25,
    @Search = NULL, @Severity = NULL, @ChannelOwner = NULL;
SET STATISTICS TIME OFF; SET STATISTICS IO OFF;
*/

-- 5) Smoke-time Detail for one row
/*
DECLARE @Id INT = (SELECT TOP (1) Id FROM dbo.TrialFlags WHERE ReviewStatus = 'pending' ORDER BY Id DESC);
EXEC dbo.usp_GetTrialFlagDetail @TrialFlagId = @Id;
*/

-- 6) Confirm old wide proc is retired (must RAISERROR)
/*
BEGIN TRY
    EXEC dbo.usp_GetTrialFlagsForReview @ReviewStatus = 'pending';
END TRY
BEGIN CATCH
    SELECT ERROR_MESSAGE() AS RetiredProcMessage;
END CATCH
*/

-- 7) Optional: recent blocking / waits (needs VIEW SERVER STATE)
/*
SELECT TOP 20 *
FROM sys.dm_os_wait_stats
WHERE wait_type LIKE 'LCK%' OR wait_type IN ('PAGEIOLATCH_SH','WRITELOG','ASYNC_NETWORK_IO')
ORDER BY wait_time_ms DESC;
*/
GO
