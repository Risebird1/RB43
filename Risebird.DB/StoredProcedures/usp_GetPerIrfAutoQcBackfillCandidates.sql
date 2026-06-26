CREATE OR ALTER PROCEDURE [dbo].[usp_GetPerIrfAutoQcBackfillCandidates]
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
    )
    SELECT
        ti.InterviewId,
        ctx.TransInterviewerId,
        ctx.IrfId,
        ti.Verdict,
        ti.VerdictFinalizedAt,
        ctx.AlreadyFlagged,
        ctx.InterviewerEmail,
        ctx.UserId,
        ctx.InterviewerCodedId
    FROM TriggeringInterviews ti
    CROSS APPLY (
        SELECT TOP 1
            tr.id AS TransInterviewerId,
            tr.IRFId AS IrfId,
            ul.EmailId AS InterviewerEmail,
            tr.UserId AS UserId,
            ip.NickName AS InterviewerCodedId,
            CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(tr.AutoQcVerdict, ''))), '') IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS AlreadyFlagged
        FROM dbo.InterviewMeetings im
        INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId AND irf.id = @IrfId
        INNER JOIN dbo.UserList ul ON ul.EmailId = im.interviewer
        INNER JOIN dbo.TransInterviewer tr ON tr.IRFId = irf.id AND tr.UserId = ul.id
        INNER JOIN dbo.InterviewerProfile ip ON ip.UserId = tr.UserId
        WHERE im.id = ti.InterviewId
          AND EXISTS (
                SELECT 1 FROM dbo.InterviewMeetings cim
                INNER JOIN dbo.IRF cirf ON cirf.IRFId = cim.IRFId AND cirf.id = @IrfId
                INNER JOIN dbo.InterviewComments cic ON cic.InterviewId = cim.id
                WHERE cim.interviewer = ul.EmailId AND cic.MeetingStatus IN (4, 6, 7, 8)
          )
    ) ctx
    WHERE ti.Verdict IS NOT NULL
      AND ctx.AlreadyFlagged = 0
    ORDER BY ctx.TransInterviewerId, ti.VerdictFinalizedAt DESC, ti.InterviewId DESC;
END
GO
