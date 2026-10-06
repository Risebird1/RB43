/*
================================================================================
QC Activity / Interviewer QC View — read indexes for the source tables.

usp_QCActivity_CollectRows filters every source by its action date (and Plivo
calls by caller). Without these indexes the dashboards scan whole tables on
each load, which makes 30–90 day ranges slow on production. The CallSid index
also serves the Plivo callbacks and coaching-session call linking.

Each index is created only when no index on the table already starts with the
same column. On editions that support it (Azure SQL Database, Managed
Instance, Enterprise) the build is ONLINE, so the table stays usable;
elsewhere it is a normal build. Run at a quiet time.
Idempotent: safe to re-run. Optional — the dashboards work without it, slower.
================================================================================
*/
SET NOCOUNT ON;

-- 3 = Enterprise/Developer, 5 = Azure SQL Database, 8 = Azure SQL Managed Instance
DECLARE @With NVARCHAR(30) =
    CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS INT) IN (3, 5, 8) THEN N' WITH (ONLINE = ON)' ELSE N'' END;

-- LeadColumn: skip the index when any index on the table already starts with it.
DECLARE @Ix TABLE (Seq INT, TableName SYSNAME, LeadColumn SYSNAME, KeyList NVARCHAR(200), IndexName SYSNAME, IncludeList NVARCHAR(400));
INSERT INTO @Ix VALUES
    (1, N'PaymentSLADetailsHistory', N'CreatedOn',     N'CreatedOn',              N'IX_PaymentSLADetailsHistory_CreatedOn',   N'Interviewid, CreatedBy, PaymentDeduction, Recommendation'),
    (2, N'PaymentSLADetails',        N'ModifiedOn',    N'ModifiedOn',             N'IX_PaymentSLADetails_ModifiedOn',         N'Interviewid, ModifiedBy, IsApproved, PaymentDeduction, Recommendation'),
    (3, N'RedMarkerStatusDetails',   N'CreatedOn',     N'CreatedOn',              N'IX_RedMarkerStatusDetails_CreatedOn',     N'[Type], [Status], CreatedBy, InterviewerUserid, IRFId'),
    (4, N'Trans_QCFormReport',       N'Addedon',       N'Addedon',                N'IX_Trans_QCFormReport_Addedon',           N'Interviewid, Userid, Redflags'),
    (5, N'TrialFlags',               N'ReviewedAtUtc', N'ReviewedAtUtc',          N'IX_TrialFlags_ReviewedAtUtc',             N'ReviewStatus, ReviewedBy, InterviewerId, IrfId'),
    (6, N'TransInterviewerAuditLog', N'ChangedAt',     N'ChangedAt',              N'IX_TransInterviewerAuditLog_ChangedAt',   N'EventType, ChangedBy, TransInterviewerId'),
    (7, N'CallRecordingData',        N'Caller',        N'Caller, DateCreated',    N'IX_CallRecordingData_Caller_DateCreated', N'CallSid, CallRecipient, RecipientProfileType, RecordingSid, Duration, [Status]'),
    (8, N'CallRecordingData',        N'CallSid',       N'CallSid',                N'IX_CallRecordingData_CallSid',            NULL);

DECLARE @Seq INT = 1, @Max INT = (SELECT MAX(Seq) FROM @Ix),
        @Table SYSNAME, @Lead SYSNAME, @Keys NVARCHAR(200), @Name SYSNAME, @Include NVARCHAR(400), @Sql NVARCHAR(MAX);
WHILE @Seq <= @Max
BEGIN
    SELECT @Table = TableName, @Lead = LeadColumn, @Keys = KeyList, @Name = IndexName, @Include = IncludeList
    FROM @Ix WHERE Seq = @Seq;

    IF OBJECT_ID(N'dbo.' + @Table, N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.' + @Table, @Lead) IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.index_columns ic
            INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE ic.object_id = OBJECT_ID(N'dbo.' + @Table) AND ic.key_ordinal = 1 AND c.name = @Lead)
    BEGIN
        SET @Sql = N'CREATE NONCLUSTERED INDEX ' + QUOTENAME(@Name) + N' ON dbo.' + QUOTENAME(@Table) + N' (' + @Keys + N')'
                 + ISNULL(N' INCLUDE (' + @Include + N')', N'') + @With + N';';
        PRINT N'Creating ' + @Name;
        EXEC sys.sp_executesql @Sql;
    END
    ELSE
        PRINT N'Skipped ' + @Name + N' (table missing or an index already starts with ' + @Lead + N')';

    SET @Seq += 1;
END
GO
