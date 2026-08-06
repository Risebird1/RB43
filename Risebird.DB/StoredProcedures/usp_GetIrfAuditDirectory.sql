CREATE OR ALTER PROCEDURE [dbo].[usp_GetIrfAuditDirectory]
    @FromIst     DATE = NULL,
    @ToIst       DATE = NULL,
    @OwnerEmail  VARCHAR(150) = NULL,
    @ProfileName VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @ToIst IS NULL
        SET @ToIst = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
    IF @FromIst IS NULL
        SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF @FromIst > @ToIst
        SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF DATEDIFF(DAY, @FromIst, @ToIst) > 90
        SET @FromIst = DATEADD(DAY, -90, @ToIst);

    DECLARE @HasAutoQcVerdict     BIT = CASE WHEN COL_LENGTH('dbo.TransInterviewer', 'AutoQcVerdict') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasBackfillTable     BIT = CASE WHEN OBJECT_ID(N'dbo.AutoQcIrfBackfillRun', N'U') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasRiskBackfillTable BIT = CASE WHEN OBJECT_ID(N'dbo.IrfAuditRiskBackfillRun', N'U') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasMergedJson    BIT = CASE WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasIsDeleted     BIT = CASE WHEN COL_LENGTH('dbo.IRF', 'IsDeleted') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @OwnerFilterActive BIT = CASE
        WHEN @ProfileName IN ('HR', 'SeniorHR') AND NULLIF(LTRIM(RTRIM(@OwnerEmail)), '') IS NOT NULL THEN 1
        ELSE 0 END;
    DECLARE @Sql NVARCHAR(MAX);

    SET @Sql = N'
    ;WITH ActiveIrfsInRange AS (
        SELECT DISTINCT irf.id AS IrfId
        FROM dbo.IRF irf
        INNER JOIN dbo.InterviewMeetings im ON im.IRFId = irf.IRFId
        INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE ic.MeetingStatus IN (4, 6, 7, 8)
          AND CAST(DATEADD(MINUTE, 330, im.interviewdate) AS DATE) >= @FromIst
          AND CAST(DATEADD(MINUTE, 330, im.interviewdate) AS DATE) <= @ToIst
          AND GETDATE() BETWEEN ISNULL(irf.StartDate, ''1900-01-01'') AND ISNULL(irf.EndDate, ''2100-01-01'')';

    IF @HasIsDeleted = 1
        SET @Sql += N'
          AND ISNULL(irf.IsDeleted, 0) = 0';

    SET @Sql += N'
    ),
    Completed AS (
        SELECT
            irf.id AS IrfId,
            irf.IRFId AS IrfName,
            MAX(irf.RisebirdHr) AS RisebirdHr,
            COUNT(DISTINCT im.id) AS TotalCompletedLifetime,
            COUNT(DISTINCT CASE WHEN im.interviewdate >= DATEADD(DAY, -90, GETUTCDATE()) THEN im.id END) AS CompletedLast90Days
        FROM dbo.IRF irf
        INNER JOIN dbo.InterviewMeetings im ON im.IRFId = irf.IRFId
        INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        INNER JOIN ActiveIrfsInRange air ON air.IrfId = irf.id
        WHERE ic.MeetingStatus IN (4, 6, 7, 8)
        GROUP BY irf.id, irf.IRFId
        HAVING COUNT(DISTINCT im.id) >= 1
    ),
    SuspiciousRedo90d AS (
        SELECT
            irf.id AS IrfId,
            COUNT(DISTINCT q.InterviewId) AS SuspiciousRedoLast90Days
        FROM dbo.QcInterviewAIevaluation q
        INNER JOIN dbo.InterviewMeetings im ON im.id = q.InterviewId
        INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
        INNER JOIN ActiveIrfsInRange air ON air.IrfId = irf.id
        WHERE ISNULL(q.QCDone, 0) = 1
          AND q.QCDoneOn IS NOT NULL
          AND CAST(q.QCDoneOn AS DATE) >= CAST(DATEADD(DAY, -90, DATEADD(MINUTE, 330, GETUTCDATE())) AS DATE)
          AND CAST(q.QCDoneOn AS DATE) <= CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE)
          AND (
                UPPER(LTRIM(RTRIM(ISNULL(q.AIMergeInterviewerSuspicious, '''')))) IN (''YES'', ''Y'', ''TRUE'')
             OR ISNULL(q.AIMergeRedoInterview, 0) = 1';

    IF @HasMergedJson = 1
        SET @Sql += N'
             OR UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(q.MergedAIQCDetails, ''$.interviewer_suspicious''), '''')))) IN (''YES'', ''Y'', ''TRUE'')
             OR ISNULL(TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, ''$.RedoInterview'') AS BIT), 0) = 1';

    SET @Sql += N'
          )
        GROUP BY irf.id
    )';

    IF @HasAutoQcVerdict = 1
        SET @Sql += N',
    PendingFlags AS (
        SELECT t.IRFId AS IrfId, COUNT(*) AS PendingFlagsCount
        FROM dbo.TransInterviewer t
        INNER JOIN ActiveIrfsInRange air ON air.IrfId = t.IRFId
        WHERE t.IsActive = 1
          AND NULLIF(LTRIM(RTRIM(ISNULL(t.AutoQcVerdict, ''''))), '''') IS NOT NULL
        GROUP BY t.IRFId
    )';

    IF @HasBackfillTable = 1 OR @HasRiskBackfillTable = 1
    BEGIN
        SET @Sql += N',
    LastBackfillCombined AS (
        SELECT IrfId, MAX(ExecutedAtUtc) AS LastBackfillAtUtc
        FROM (';

        DECLARE @HasUnionBranch BIT = 0;
        IF @HasBackfillTable = 1
        BEGIN
            SET @Sql += N'
            SELECT IrfId, ExecutedAtUtc FROM dbo.AutoQcIrfBackfillRun';
            SET @HasUnionBranch = 1;
        END
        IF @HasRiskBackfillTable = 1
        BEGIN
            IF @HasUnionBranch = 1
                SET @Sql += N'
            UNION ALL';
            SET @Sql += N'
            SELECT IrfId, ExecutedAtUtc FROM dbo.IrfAuditRiskBackfillRun';
        END

        SET @Sql += N'
        ) u
        GROUP BY IrfId
    )';
    END

    IF @HasRiskBackfillTable = 1
        SET @Sql += N',
    LastRiskBackfillRow AS (
        SELECT IrfId, ExecutedAtUtc, FromIst, ToIst,
               ROW_NUMBER() OVER (PARTITION BY IrfId ORDER BY ExecutedAtUtc DESC) AS rn
        FROM dbo.IrfAuditRiskBackfillRun
    )';

    SET @Sql += N'
    SELECT
        c.IrfId,
        c.IrfName,
        ISNULL(c.TotalCompletedLifetime, 0) AS TotalCompletedLifetime,
        ISNULL(c.CompletedLast90Days, 0) AS CompletedLast90Days,
        ISNULL(sr.SuspiciousRedoLast90Days, 0) AS SuspiciousRedoLast90Days,';

    IF @HasAutoQcVerdict = 1
        SET @Sql += N'
        ISNULL(p.PendingFlagsCount, 0) AS PendingFlagsCount,';
    ELSE
        SET @Sql += N'
        CAST(0 AS INT) AS PendingFlagsCount,';

    IF @HasBackfillTable = 1 OR @HasRiskBackfillTable = 1
        SET @Sql += N'
        lb.LastBackfillAtUtc,';
    ELSE
        SET @Sql += N'
        CAST(NULL AS DATETIME) AS LastBackfillAtUtc,';

    IF @HasRiskBackfillTable = 1
        SET @Sql += N'
        lrb.FromIst AS LastRiskBackfillFromIst,
        lrb.ToIst AS LastRiskBackfillToIst,';
    ELSE
        SET @Sql += N'
        CAST(NULL AS DATE) AS LastRiskBackfillFromIst,
        CAST(NULL AS DATE) AS LastRiskBackfillToIst,';

    SET @Sql += N'
        NULLIF(LTRIM(RTRIM(ulco.EmailId)), '''') AS ChannelOwnerEmail,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(ulco.EmailId)), '''') IS NULL THEN NULL
            WHEN ulco.LastName IS NULL AND NULLIF(LTRIM(RTRIM(ulco.FirstName)), '''') IS NOT NULL
                THEN LTRIM(RTRIM(ulco.FirstName)) + N'' ('' + LTRIM(RTRIM(ulco.EmailId)) + N'')''
            WHEN NULLIF(LTRIM(RTRIM(ulco.FirstName)), '''') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ulco.LastName)), '''') IS NOT NULL
                THEN LTRIM(RTRIM(CONCAT(ISNULL(ulco.FirstName, ''''), N'' '', ISNULL(ulco.LastName, ''''))))
                     + N'' ('' + LTRIM(RTRIM(ulco.EmailId)) + N'')''
            ELSE LTRIM(RTRIM(ulco.EmailId))
        END AS ChannelOwnerDisplay
    FROM Completed c
    LEFT JOIN SuspiciousRedo90d sr ON sr.IrfId = c.IrfId
    LEFT JOIN dbo.UserList ulco ON ulco.id = c.RisebirdHr';

    IF @HasAutoQcVerdict = 1
        SET @Sql += N'
    LEFT JOIN PendingFlags p ON p.IrfId = c.IrfId';

    IF @HasBackfillTable = 1 OR @HasRiskBackfillTable = 1
        SET @Sql += N'
    LEFT JOIN LastBackfillCombined lb ON lb.IrfId = c.IrfId';

    IF @HasRiskBackfillTable = 1
        SET @Sql += N'
    LEFT JOIN LastRiskBackfillRow lrb ON lrb.IrfId = c.IrfId AND lrb.rn = 1';

    IF @OwnerFilterActive = 1
        SET @Sql += N'
    WHERE ulco.EmailId = @OwnerEmail';

    IF @HasAutoQcVerdict = 1
        SET @Sql += N'
    ORDER BY ISNULL(p.PendingFlagsCount, 0) DESC, ISNULL(sr.SuspiciousRedoLast90Days, 0) DESC, c.IrfName;';
    ELSE
        SET @Sql += N'
    ORDER BY ISNULL(sr.SuspiciousRedoLast90Days, 0) DESC, c.IrfName;';

    EXEC sys.sp_executesql @Sql,
        N'@FromIst DATE, @ToIst DATE, @OwnerEmail VARCHAR(150)',
        @FromIst = @FromIst, @ToIst = @ToIst, @OwnerEmail = @OwnerEmail;
END
GO
