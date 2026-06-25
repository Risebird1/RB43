CREATE OR ALTER PROCEDURE [dbo].[usp_GetAutoQcBackfillCandidates]
    @AsOfUtc DATETIME = NULL,
    @WindowDays INT = 60
AS
BEGIN
    SET NOCOUNT ON;

    IF @AsOfUtc IS NULL
        SET @AsOfUtc = GETUTCDATE();

    DECLARE @WindowStart DATETIME = DATEADD(DAY, -@WindowDays, @AsOfUtc);

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
        WHERE ISNULL(q.QCDone, 0) = 1
          AND q.QCDoneOn IS NOT NULL
          AND q.QCDoneOn >= @WindowStart
          AND q.QCDoneOn <= @AsOfUtc
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
        ctx.RbVetAlreadyNo,
        ctx.InterviewerEmail,
        ctx.UserId,
        ctx.InterviewerCodedId
    FROM TriggeringInterviews ti
    CROSS APPLY (
        SELECT TOP 1
            c.TransInterviewerId,
            c.IrfId,
            c.InterviewerEmail,
            c.UserId,
            c.InterviewerCodedId,
            c.RbVetAlreadyNo
        FROM (
            SELECT
                t.id AS TransInterviewerId,
                t.IRFId AS IrfId,
                ul.EmailId AS InterviewerEmail,
                t.UserId AS UserId,
                ip.NickName AS InterviewerCodedId,
                CASE
                    WHEN t.IsSMEVerified = 0 THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                END AS RbVetAlreadyNo
            FROM dbo.InterviewMeetings im
            INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
            INNER JOIN dbo.UserList ul ON ul.EmailId = im.interviewer
            INNER JOIN dbo.TransInterviewer t ON t.IRFId = irf.id AND t.UserId = ul.id
            INNER JOIN dbo.InterviewerProfile ip ON ip.UserId = t.UserId
            WHERE im.id = ti.InterviewId
        ) c
    ) ctx
    WHERE ti.Verdict IS NOT NULL
    ORDER BY ctx.TransInterviewerId, ti.VerdictFinalizedAt DESC, ti.InterviewId DESC;
END
GO
