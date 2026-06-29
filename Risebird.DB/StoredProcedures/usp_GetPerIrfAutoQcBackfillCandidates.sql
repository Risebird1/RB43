CREATE OR ALTER PROCEDURE [dbo].[usp_GetPerIrfAutoQcBackfillCandidates]

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

        RETURN;



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

        VerdictFinalizedAt DATETIME NOT NULL,

        Verdict VARCHAR(20) NOT NULL

    );



    INSERT INTO #Triggering (InterviewId, VerdictFinalizedAt, Verdict)

    SELECT

        q.InterviewId,

        q.QCDoneOn,

        CASE

            WHEN UPPER(LTRIM(RTRIM(ISNULL(q.AIMergeInterviewerSuspicious, '')))) IN ('YES', 'Y', 'TRUE') THEN 'suspicious'

            WHEN ISNULL(q.AIMergeRedoInterview, 0) = 1 THEN 'redo'

            ELSE NULL

        END

    FROM dbo.QcInterviewAIevaluation q

    INNER JOIN #QcDone d ON d.InterviewId = q.InterviewId

    WHERE UPPER(LTRIM(RTRIM(ISNULL(q.AIMergeInterviewerSuspicious, '')))) IN ('YES', 'Y', 'TRUE')

       OR ISNULL(q.AIMergeRedoInterview, 0) = 1;



    IF @HasMergedJson = 1

    BEGIN

        INSERT INTO #Triggering (InterviewId, VerdictFinalizedAt, Verdict)

        SELECT

            q.InterviewId,

            q.QCDoneOn,

            CASE

                WHEN UPPER(LTRIM(RTRIM(ISNULL(JSON_VALUE(q.MergedAIQCDetails, '$.interviewer_suspicious'), '')))) IN ('YES', 'Y', 'TRUE') THEN 'suspicious'

                WHEN ISNULL(TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.RedoInterview') AS BIT), 0) = 1 THEN 'redo'

                ELSE NULL

            END

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



    ;WITH Interviewers AS (

        SELECT ti.InterviewId, ti.Verdict, ti.VerdictFinalizedAt, LTRIM(RTRIM(im.interviewer)) AS InterviewerRaw

        FROM #Triggering ti

        INNER JOIN dbo.InterviewMeetings im ON im.id = ti.InterviewId

        INNER JOIN #Completed c ON c.InterviewId = ti.InterviewId

        WHERE NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL

    ),

    ResolvedUsers AS (

        SELECT

            iv.InterviewId,

            iv.Verdict,

            iv.VerdictFinalizedAt,

            u.UserId,

            u.EmailId,

            ROW_NUMBER() OVER (PARTITION BY iv.InterviewId ORDER BY u.Priority) AS rn

        FROM Interviewers iv

        CROSS APPLY (

            SELECT ul.id AS UserId, ul.EmailId, 1 AS Priority

            FROM dbo.UserList ul

            WHERE ul.EmailId = iv.InterviewerRaw

               OR LTRIM(RTRIM(ul.EmailId)) = iv.InterviewerRaw

            UNION ALL

            SELECT ip.UserId, ul.EmailId, 2

            FROM dbo.InterviewerProfile ip

            INNER JOIN dbo.UserList ul ON ul.id = ip.UserId

            WHERE ip.NickName = iv.InterviewerRaw

               OR LTRIM(RTRIM(ip.NickName)) = iv.InterviewerRaw

        ) u

    ),

    Mapped AS (

        SELECT

            ru.InterviewId,

            MIN(tu.TransInterviewerId) AS TransInterviewerId,

            ru.Verdict,

            ru.VerdictFinalizedAt,

            ru.UserId,

            ru.EmailId

        FROM ResolvedUsers ru

        INNER JOIN #TransUsers tu ON tu.UserId = ru.UserId

        WHERE ru.rn = 1

        GROUP BY ru.InterviewId, ru.Verdict, ru.VerdictFinalizedAt, ru.UserId, ru.EmailId

    )

    SELECT

        m.InterviewId,

        m.TransInterviewerId,

        @IrfId AS IrfId,

        m.Verdict,

        m.VerdictFinalizedAt,

        CAST(0 AS BIT) AS AlreadyFlagged,

        m.EmailId AS InterviewerEmail,

        m.UserId,

        ip.NickName AS InterviewerCodedId,

        tr.AutoQcVerdict AS CurrentAutoQcVerdict

    FROM Mapped m

    INNER JOIN dbo.TransInterviewer tr ON tr.id = m.TransInterviewerId

    LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = m.UserId

    WHERE NOT EXISTS (

        SELECT 1

        FROM #Audited a

        WHERE a.TransInterviewerId = m.TransInterviewerId

          AND a.TriggeringInterviewId = m.InterviewId

    )

    ORDER BY m.TransInterviewerId, m.VerdictFinalizedAt DESC, m.InterviewId DESC;

END

GO
