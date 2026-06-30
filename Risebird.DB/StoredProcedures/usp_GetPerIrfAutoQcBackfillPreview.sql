CREATE OR ALTER PROCEDURE [dbo].[usp_GetPerIrfAutoQcBackfillPreview]

    @IrfId   INT,

    @FromUtc DATETIME,

    @ToUtc   DATETIME

AS

BEGIN

    SET NOCOUNT ON;



    IF @FromUtc IS NULL SET @FromUtc = DATEADD(DAY, -90, GETUTCDATE());

    IF @ToUtc IS NULL SET @ToUtc = GETUTCDATE();



    DECLARE @FromDate DATE = CAST(@FromUtc AS DATE);

    DECLARE @ToDate   DATE = CAST(@ToUtc AS DATE);

    DECLARE @IrfVarchar VARCHAR(200);

    DECLARE @HasMergedJson BIT = CASE WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NOT NULL THEN 1 ELSE 0 END;



    SELECT @IrfVarchar = irf.IRFId

    FROM dbo.IRF irf

    WHERE irf.id = @IrfId;



    IF @IrfVarchar IS NULL

    BEGIN

        SELECT

            0 AS VerdictCount,

            0 AS WouldFlagCount,

            0 AS AlreadyFlaggedCount,

            0 AS UnmappedVerdictCount,

            0 AS QcMergedInRangeCount,

            CAST(NULL AS DATE) AS OldestVerdictDate,

            CAST(NULL AS DATE) AS NewestVerdictDate;

        RETURN;

    END



    CREATE TABLE #QcDone (

        InterviewId INT NOT NULL PRIMARY KEY,

        VerdictFinalizedAt DATETIME NOT NULL

    );



    INSERT INTO #QcDone (InterviewId, VerdictFinalizedAt)

    SELECT q.InterviewId, q.QCDoneOn

    FROM dbo.QcInterviewAIevaluation q

    INNER JOIN dbo.InterviewMeetings im ON im.id = q.InterviewId AND im.IRFId = @IrfVarchar

    WHERE ISNULL(q.QCDone, 0) = 1

      AND q.QCDoneOn IS NOT NULL

      AND CAST(q.QCDoneOn AS DATE) >= @FromDate

      AND CAST(q.QCDoneOn AS DATE) <= @ToDate

      AND q.InterviewId IS NOT NULL;



    CREATE TABLE #Triggering (

        InterviewId INT NOT NULL PRIMARY KEY,

        VerdictFinalizedAt DATETIME NOT NULL

    );



    -- Column-based verdicts (same as merge job persisted fields)

    INSERT INTO #Triggering (InterviewId, VerdictFinalizedAt)

    SELECT q.InterviewId, q.QCDoneOn

    FROM dbo.QcInterviewAIevaluation q

    INNER JOIN #QcDone d ON d.InterviewId = q.InterviewId

    WHERE UPPER(LTRIM(RTRIM(ISNULL(q.AIMergeInterviewerSuspicious, '')))) IN ('YES', 'Y', 'TRUE')

       OR ISNULL(q.AIMergeRedoInterview, 0) = 1;



    -- JSON fallback only for merges already in range (matches directory logic, stays fast)

    IF @HasMergedJson = 1

    BEGIN

        INSERT INTO #Triggering (InterviewId, VerdictFinalizedAt)

        SELECT q.InterviewId, q.QCDoneOn

        FROM dbo.QcInterviewAIevaluation q

        INNER JOIN #QcDone d ON d.InterviewId = q.InterviewId

        WHERE NOT EXISTS (SELECT 1 FROM #Triggering t WHERE t.InterviewId = q.InterviewId)

          AND (

                UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(q.MergedAIQCDetails, '$.interviewer_suspicious'), '')))) IN ('YES', 'Y', 'TRUE')

             OR ISNULL(TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.RedoInterview') AS BIT), 0) = 1

          );

    END



    CREATE TABLE #Completed (

        InterviewId INT NOT NULL PRIMARY KEY

    );



    INSERT INTO #Completed (InterviewId)

    SELECT DISTINCT ti.InterviewId

    FROM #Triggering ti

    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = ti.InterviewId

    WHERE ic.MeetingStatus IN (4, 6, 7, 8);



    CREATE TABLE #TransUsers (

        TransInterviewerId INT NOT NULL,

        UserId INT NOT NULL,

        PRIMARY KEY (TransInterviewerId)

    );



    INSERT INTO #TransUsers (TransInterviewerId, UserId)

    SELECT

        tr.id,

        COALESCE(tr.UserId, iv.UserId, ipr.UserId)

    FROM dbo.TransInterviewer tr

    LEFT JOIN dbo.Interviewer iv ON iv.id = tr.InterviewerId

    LEFT JOIN dbo.InterviewerProfile ipr ON ipr.id = tr.InterviewerId

    WHERE tr.IRFId = @IrfId

      AND COALESCE(tr.UserId, iv.UserId, ipr.UserId) IS NOT NULL;



    CREATE TABLE #Resolved (

        InterviewId INT NOT NULL PRIMARY KEY,

        UserId INT NOT NULL,

        TransInterviewerId INT NULL

    );



    ;WITH Interviewers AS (

        SELECT ti.InterviewId, LTRIM(RTRIM(im.interviewer)) AS InterviewerRaw

        FROM #Triggering ti

        INNER JOIN dbo.InterviewMeetings im ON im.id = ti.InterviewId

        WHERE NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL

    ),

    ResolvedUsers AS (

        SELECT

            iv.InterviewId,

            u.UserId,

            ROW_NUMBER() OVER (PARTITION BY iv.InterviewId ORDER BY u.Priority) AS rn

        FROM Interviewers iv

        CROSS APPLY (

            SELECT ul.id AS UserId, 1 AS Priority

            FROM dbo.UserList ul

            WHERE ul.EmailId = iv.InterviewerRaw

               OR LTRIM(RTRIM(ul.EmailId)) = iv.InterviewerRaw

            UNION ALL

            SELECT ip.UserId, 2

            FROM dbo.InterviewerProfile ip

            WHERE ip.NickName = iv.InterviewerRaw

               OR LTRIM(RTRIM(ip.NickName)) = iv.InterviewerRaw

        ) u

    ),

    MappedUsers AS (

        SELECT

            ru.InterviewId,

            ru.UserId,

            MIN(tu.TransInterviewerId) AS TransInterviewerId

        FROM ResolvedUsers ru

        LEFT JOIN #TransUsers tu ON tu.UserId = ru.UserId

        WHERE ru.rn = 1

        GROUP BY ru.InterviewId, ru.UserId

    )

    INSERT INTO #Resolved (InterviewId, UserId, TransInterviewerId)

    SELECT InterviewId, UserId, TransInterviewerId

    FROM MappedUsers;



    CREATE TABLE #Audited (

        TransInterviewerId INT NOT NULL,

        TriggeringInterviewId INT NOT NULL,

        PRIMARY KEY (TransInterviewerId, TriggeringInterviewId)

    );



    IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NOT NULL

    BEGIN

        INSERT INTO #Audited (TransInterviewerId, TriggeringInterviewId)

        SELECT DISTINCT al.TransInterviewerId, al.TriggeringInterviewId

        FROM dbo.TransInterviewerAuditLog al

        INNER JOIN #Triggering t ON t.InterviewId = al.TriggeringInterviewId

        WHERE al.EventType = 'AutoQcFlag'

          AND al.TriggeringInterviewId IS NOT NULL;

    END



    CREATE TABLE #Mapped (

        InterviewId INT NOT NULL,

        TransInterviewerId INT NOT NULL,

        AlreadyAudited BIT NOT NULL,

        PRIMARY KEY (InterviewId, TransInterviewerId)

    );



    INSERT INTO #Mapped (InterviewId, TransInterviewerId, AlreadyAudited)

    SELECT

        r.InterviewId,

        r.TransInterviewerId,

        CASE WHEN a.TransInterviewerId IS NOT NULL THEN 1 ELSE 0 END

    FROM #Resolved r

    INNER JOIN #Completed c ON c.InterviewId = r.InterviewId

    LEFT JOIN #Audited a

        ON a.TransInterviewerId = r.TransInterviewerId

       AND a.TriggeringInterviewId = r.InterviewId

    WHERE r.TransInterviewerId IS NOT NULL;



    SELECT

        (SELECT COUNT(*) FROM #Triggering) AS VerdictCount,

        (SELECT COUNT(DISTINCT TransInterviewerId) FROM #Mapped WHERE AlreadyAudited = 0) AS WouldFlagCount,

        (SELECT COUNT(DISTINCT TransInterviewerId) FROM #Mapped WHERE AlreadyAudited = 1) AS AlreadyFlaggedCount,

        (SELECT COUNT(*) FROM #Triggering t

         WHERE NOT EXISTS (

            SELECT 1 FROM #Resolved r

            WHERE r.InterviewId = t.InterviewId AND r.TransInterviewerId IS NOT NULL)) AS UnmappedVerdictCount,

        (SELECT COUNT(*) FROM #QcDone) AS QcMergedInRangeCount,

        (SELECT MIN(CAST(VerdictFinalizedAt AS DATE)) FROM #Triggering) AS OldestVerdictDate,

        (SELECT MAX(CAST(VerdictFinalizedAt AS DATE)) FROM #Triggering) AS NewestVerdictDate;

END

GO
