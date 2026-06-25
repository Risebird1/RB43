CREATE OR ALTER PROCEDURE [dbo].[usp_GetAutoQcBackfillCandidateCount]

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

    ),

    DistinctTrans AS (

        SELECT DISTINCT ctx.TransInterviewerId

        FROM TriggeringInterviews ti

        CROSS APPLY (

            SELECT TOP 1 t.id AS TransInterviewerId

            FROM dbo.InterviewMeetings im

            INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId

            INNER JOIN dbo.TransInterviewer t ON t.IRFId = irf.id

            INNER JOIN dbo.UserList ul ON ul.id = t.UserId AND ul.EmailId = im.interviewer

            WHERE im.id = ti.InterviewId

        ) ctx

        WHERE ti.Verdict IS NOT NULL

    )

    SELECT COUNT(1) AS CandidateTransInterviewerCount

    FROM DistinctTrans;

END

GO

