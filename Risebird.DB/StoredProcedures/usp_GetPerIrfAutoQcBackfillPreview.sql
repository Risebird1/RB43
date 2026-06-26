CREATE OR ALTER PROCEDURE [dbo].[usp_GetPerIrfAutoQcBackfillPreview]
    @IrfId   INT,
    @FromUtc DATETIME,
    @ToUtc   DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromUtc IS NULL SET @FromUtc = DATEADD(DAY, -60, GETUTCDATE());
    IF @ToUtc IS NULL SET @ToUtc = GETUTCDATE();

    ;WITH TriggeringInterviews AS (
        SELECT
            q.InterviewId,
            q.QCDoneOn AS VerdictFinalizedAt,
            CASE
                WHEN UPPER(LTRIM(RTRIM(ISNULL(q.AIMergeInterviewerSuspicious, '')))) = 'YES' THEN 'suspicious'
                WHEN ISNULL(q.AIMergeRedoInterview, 0) = 1 THEN 'redo'
                ELSE NULL
            END AS Verdict
        FROM dbo.QcInterviewAIevaluation q
        INNER JOIN dbo.InterviewMeetings im ON im.id = q.InterviewId
        INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId AND irf.id = @IrfId
        WHERE ISNULL(q.QCDone, 0) = 1
          AND q.QCDoneOn IS NOT NULL
          AND q.QCDoneOn >= @FromUtc
          AND q.QCDoneOn <= @ToUtc
          AND q.InterviewId IS NOT NULL
          AND (
                UPPER(LTRIM(RTRIM(ISNULL(q.AIMergeInterviewerSuspicious, '')))) = 'YES'
             OR ISNULL(q.AIMergeRedoInterview, 0) = 1
          )
    ),
    Mapped AS (
        SELECT DISTINCT
            ti.InterviewId,
            ti.Verdict,
            ctx.TransInterviewerId,
            CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.AutoQcVerdict, ''))), '') IS NOT NULL THEN 1 ELSE 0 END AS AlreadyFlagged,
            CASE WHEN EXISTS (
                SELECT 1 FROM dbo.InterviewMeetings cim
                INNER JOIN dbo.IRF cirf ON cirf.IRFId = cim.IRFId AND cirf.id = @IrfId
                INNER JOIN dbo.UserList cul ON cul.EmailId = cim.interviewer AND cul.id = t.UserId
                INNER JOIN dbo.InterviewComments cic ON cic.InterviewId = cim.id
                WHERE cic.MeetingStatus IN (4, 6, 7, 8)
            ) THEN 1 ELSE 0 END AS HasCompletedOnIrf
        FROM TriggeringInterviews ti
        CROSS APPLY (
            SELECT TOP 1
                tr.id AS TransInterviewerId
            FROM dbo.InterviewMeetings im
            INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId AND irf.id = @IrfId
            INNER JOIN dbo.UserList ul ON ul.EmailId = im.interviewer
            INNER JOIN dbo.TransInterviewer tr ON tr.IRFId = irf.id AND tr.UserId = ul.id
            WHERE im.id = ti.InterviewId
        ) ctx
        INNER JOIN dbo.TransInterviewer t ON t.id = ctx.TransInterviewerId
    )
    SELECT
        COUNT(DISTINCT InterviewId) AS VerdictCount,
        COUNT(DISTINCT CASE WHEN HasCompletedOnIrf = 1 AND AlreadyFlagged = 0 THEN TransInterviewerId END) AS WouldFlagCount,
        COUNT(DISTINCT CASE WHEN HasCompletedOnIrf = 1 AND AlreadyFlagged = 1 THEN TransInterviewerId END) AS AlreadyFlaggedCount
    FROM Mapped;
END
GO