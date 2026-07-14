-- Trial Flag Review — why is dbo.TrialFlags empty?
-- Run each section on the target database.

PRINT '=== 1. Table exists? ===';
SELECT
    CASE WHEN OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL THEN 'YES' ELSE 'NO — run Tables/TrialFlags.sql' END AS TrialFlagsTable,
    (SELECT COUNT(*) FROM dbo.TrialFlags) AS TrialFlagsRowCount;

PRINT '=== 2. Required stored procedures deployed? ===';
SELECT name AS MissingProcedure
FROM (VALUES
    ('usp_GetTrialJudgmentContext'),
    ('usp_InsertTrialFlag'),
    ('usp_GetTrialFlagsForReview'),
    ('usp_UpdateTrialFlagReview')
) AS p(name)
WHERE OBJECT_ID(N'dbo.' + p.name, N'P') IS NULL;

PRINT '=== 3. QC merges completed (producer prerequisite) ===';
SELECT
    COUNT(*) AS TotalQcEvalRows,
    SUM(CASE WHEN ISNULL(QCDone, 0) = 1 THEN 1 ELSE 0 END) AS QcMergeDoneCount,
    SUM(CASE WHEN ISNULL(QCDone, 0) = 0 AND ReportAIDetails IS NOT NULL AND TranscritAiDetails IS NOT NULL THEN 1 ELSE 0 END) AS PendingMergeCount
FROM dbo.QcInterviewAIevaluation;

PRINT '=== 4. First completed interviews per (interviewer, IRF) with QC done but NO trial flag ===';
;WITH CompletedOnIrf AS (
    SELECT
        im.id AS InterviewId,
        im.IRFId,
        LTRIM(RTRIM(im.interviewer)) AS InterviewerEmail,
        im.interviewDate,
        irf.id AS IrfIntId,
        ROW_NUMBER() OVER (
            PARTITION BY im.IRFId, LTRIM(RTRIM(im.interviewer))
            ORDER BY im.interviewDate ASC, im.id ASC
        ) AS rn
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    WHERE im.confirmationStatus = 1
      AND ic.InterviewStatus IN (2, 3, 4, 5)
),
FirstOnly AS (
    SELECT c.InterviewId, c.IrfIntId, c.InterviewerEmail, c.interviewDate, c.IRFId
    FROM CompletedOnIrf c
    WHERE c.rn = 1
),
Mapped AS (
    SELECT
        f.InterviewId,
        f.IrfIntId,
        f.InterviewerEmail,
        f.interviewDate,
        f.IRFId,
        ti.id AS ChannelId,
        ti.InterviewerId
    FROM FirstOnly f
    INNER JOIN dbo.IRF irf ON irf.id = f.IrfIntId
    CROSS APPLY (
        SELECT TOP 1 u.UserId
        FROM (
            SELECT ul.id AS UserId, 1 AS Priority
            FROM dbo.UserList ul
            WHERE LTRIM(RTRIM(ul.EmailId)) = f.InterviewerEmail
            UNION ALL
            SELECT ul.id, 2
            FROM dbo.UserList ul
            INNER JOIN dbo.InterviewMeetings im ON im.id = f.InterviewId
            WHERE ul.EmailId = im.interviewer
            UNION ALL
            SELECT ip.UserId, 3
            FROM dbo.InterviewerProfile ip
            INNER JOIN dbo.UserList ul ON ul.id = ip.UserId
            WHERE LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = f.InterviewerEmail
        ) u
        ORDER BY u.Priority
    ) ru
    INNER JOIN dbo.TransInterviewer ti ON ti.IRFId = irf.id
       AND (
            ti.UserId = ru.UserId
            OR EXISTS (SELECT 1 FROM dbo.Interviewer iv WHERE iv.id = ti.InterviewerId AND iv.UserId = ru.UserId)
            OR EXISTS (SELECT 1 FROM dbo.InterviewerProfile ipr WHERE ipr.id = ti.InterviewerId AND ipr.UserId = ru.UserId)
       )
)
SELECT
    COUNT(*) AS BackfillCandidateCount
FROM Mapped m
INNER JOIN dbo.QcInterviewAIevaluation q ON q.InterviewId = m.InterviewId AND ISNULL(q.QCDone, 0) = 1
WHERE m.ChannelId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.TrialFlags tf
      WHERE tf.InterviewerId = m.InterviewerId AND tf.IrfId = m.IrfIntId
  );

PRINT '=== 5. Sample backfill candidates (top 20) ===';
;WITH CompletedOnIrf AS (
    SELECT im.id AS InterviewId, im.IRFId, LTRIM(RTRIM(im.interviewer)) AS InterviewerEmail,
           im.interviewDate, irf.id AS IrfIntId,
           ROW_NUMBER() OVER (PARTITION BY im.IRFId, LTRIM(RTRIM(im.interviewer)) ORDER BY im.interviewDate ASC, im.id ASC) AS rn
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    WHERE im.confirmationStatus = 1 AND ic.InterviewStatus IN (2, 3, 4, 5)
),
FirstOnly AS (SELECT * FROM CompletedOnIrf WHERE rn = 1),
Mapped AS (
    SELECT f.InterviewId, f.IrfIntId, f.InterviewerEmail, f.interviewDate, f.IRFId,
           ti.id AS ChannelId, ti.InterviewerId
    FROM FirstOnly f
    INNER JOIN dbo.IRF irf ON irf.id = f.IrfIntId
    CROSS APPLY (
        SELECT TOP 1 u.UserId
        FROM (
            SELECT ul.id AS UserId, 1 AS Priority
            FROM dbo.UserList ul
            WHERE LTRIM(RTRIM(ul.EmailId)) = f.InterviewerEmail
            UNION ALL
            SELECT ul.id, 2
            FROM dbo.UserList ul
            INNER JOIN dbo.InterviewMeetings im ON im.id = f.InterviewId
            WHERE ul.EmailId = im.interviewer
            UNION ALL
            SELECT ip.UserId, 3
            FROM dbo.InterviewerProfile ip
            INNER JOIN dbo.UserList ul ON ul.id = ip.UserId
            WHERE LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = f.InterviewerEmail
        ) u
        ORDER BY u.Priority
    ) ru
    INNER JOIN dbo.TransInterviewer ti ON ti.IRFId = irf.id
       AND (
            ti.UserId = ru.UserId
            OR EXISTS (SELECT 1 FROM dbo.Interviewer iv WHERE iv.id = ti.InterviewerId AND iv.UserId = ru.UserId)
            OR EXISTS (SELECT 1 FROM dbo.InterviewerProfile ipr WHERE ipr.id = ti.InterviewerId AND ipr.UserId = ru.UserId)
       )
)
SELECT TOP 20
    m.InterviewId,
    m.IRFId,
    m.InterviewerEmail,
    m.interviewDate,
    CASE WHEN m.ChannelId IS NULL THEN 'MISSING TransInterviewer mapping' ELSE 'OK' END AS MappingStatus,
    q.QCDoneOn
FROM Mapped m
INNER JOIN dbo.QcInterviewAIevaluation q ON q.InterviewId = m.InterviewId AND ISNULL(q.QCDone, 0) = 1
WHERE NOT EXISTS (SELECT 1 FROM dbo.TrialFlags tf WHERE tf.InterviewerId = m.InterviewerId AND tf.IrfId = m.IrfIntId)
ORDER BY m.interviewDate DESC;

PRINT '=== 6. Test trial judgment + TransInterviewer context for one interview ===';
DECLARE @InterviewId INT = NULL; -- SET e.g. 654192
IF @InterviewId IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.usp_GetTransInterviewerContextForInterview', N'P') IS NOT NULL
        EXEC dbo.usp_GetTransInterviewerContextForInterview @InterviewId = @InterviewId;
    IF OBJECT_ID(N'dbo.usp_GetTrialJudgmentContext', N'P') IS NOT NULL
        EXEC dbo.usp_GetTrialJudgmentContext @InterviewId = @InterviewId;
END
ELSE
    PRINT 'Set @InterviewId (e.g. 654192) to debug mapping failures.';

PRINT '=== 7. Common root causes ===';
PRINT 'A) Producer never ran — Azure QcInterviewAIMergeJob timer did not call trial judgment (fix: redeploy Azure Functions).';
PRINT 'B) Historical merges — QCDone=1 rows never re-enter pending queue; run POST /Dashboard/BackfillTrialFlags or HowTo_BackfillTrialFlags.sql candidates.';
PRINT 'C) Not first interview — only the earliest completed interview per (interviewer email, IRF) qualifies.';
PRINT 'D) TransInterviewer mapping missing — interviewer email must match UserList on a TransInterviewer row for that IRF.';
PRINT 'E) SPs not deployed — usp_GetTrialJudgmentContext / usp_InsertTrialFlag must exist on this database.';

PRINT '=== 8. Risk category linkage (Trial Flag Review risk column) ===';
SELECT
    CASE WHEN OBJECT_ID(N'dbo.RiskCategory', N'U') IS NOT NULL THEN 'YES' ELSE 'NO — run Tables/RiskCategory.sql' END AS RiskCategoryTable,
    CASE WHEN COL_LENGTH(N'dbo.RiskCategory', N'Severity') IS NOT NULL THEN 'YES' ELSE 'NO — run Tables/RiskCategory.sql (Severity column)' END AS SeverityColumn,
    (SELECT COUNT(*) FROM dbo.RiskCategory) AS RiskCategoryRowCount;

SELECT name AS MissingRiskProcedure
FROM (VALUES
    ('usp_AddUpdateRiskCategory'),
    ('usp_GetRiskCategoryByInterviewId')
) AS p(name)
WHERE OBJECT_ID(N'dbo.' + p.name, N'P') IS NULL;

;WITH Flagged AS (
    SELECT tf.TriggeringInterviewId, tf.OverallVerdict, tf.Severity AS TrialFlagSeverity
    FROM dbo.TrialFlags tf
)
SELECT
    COUNT(*) AS TrialFlagCount,
    SUM(CASE WHEN rc.InterviewId IS NOT NULL THEN 1 ELSE 0 END) AS WithRiskCategoryRow,
    SUM(CASE WHEN ISNULL(LTRIM(RTRIM(rc.Description)), '') <> '' THEN 1 ELSE 0 END) AS WithRiskDescription
FROM Flagged f
LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = f.TriggeringInterviewId;

PRINT '=== 9. Sample trial flags missing RiskCategory (top 20) ===';
SELECT TOP 20
    tf.Id AS TrialFlagId,
    tf.TriggeringInterviewId,
    tf.OverallVerdict,
    tf.Severity AS StoredTrialSeverity,
    rc.Name AS RiskLabels,
    rc.Severity AS RiskSeverity
FROM dbo.TrialFlags tf
LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = tf.TriggeringInterviewId
WHERE rc.InterviewId IS NULL OR ISNULL(LTRIM(RTRIM(rc.Description)), '') = ''
ORDER BY tf.FlaggedAtUtc DESC;

PRINT '=== 10. Fix empty risk column ===';
PRINT '1) Deploy RiskCategory.sql, usp_AddUpdateRiskCategory.sql, usp_GetTrialFlagsForReview.sql on this DB.';
PRINT '2) For each TriggeringInterviewId: EXEC usp_AddUpdateRiskCategory @InterviewId = <id>';
PRINT '3) Reload /Dashboard/TrialFlagReview (page refresh now re-runs risk producer per row).';
PRINT '4) New trial flags get risk at QC merge only if web app + Azure Functions run trial-riskcategory-link code.';
