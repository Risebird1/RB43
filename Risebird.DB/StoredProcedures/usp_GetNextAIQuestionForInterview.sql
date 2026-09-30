IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetNextAIQuestionForInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNextAIQuestionForInterview
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- The set-bound counterpart of usp_GetRandomQuestionForSession: serves the next question from the
-- one set bound to this interview, never from the wider channel pool. Output columns match that
-- procedure exactly so the runtime flow binds the result unchanged.
--
-- No bound set means no rows. The caller must stop rather than widen the query -- serving the whole
-- pool would defeat the anti-leak property that the sets exist to provide.
--
-- Code project questions are served only once CodeProjectDetails.ValidationStatus = 'passed'.

CREATE PROCEDURE [dbo].[usp_GetNextAIQuestionForInterview]
    @interviewId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SessionId UNIQUEIDENTIFIER = (SELECT SessionId FROM InterviewMeetings WHERE id = @interviewId);
    DECLARE @SetId int = (SELECT SetId FROM InterviewAIQuestionSet WHERE InterviewId = @interviewId);

    IF @SetId IS NULL OR @SessionId IS NULL
        RETURN;

    DECLARE @AskedCount INT = (
        SELECT COUNT(*)
        FROM TranscriptsConv
        WHERE SessionId = @SessionId AND [Role] = 'assistant'
    );

    ;WITH SetQuestions AS (
        SELECT q.id, q.Comment, q.AIQuestionVideoLink, q.IsTypingSpeedAssessment,
               q.IsListeningComprehension, q.ListeningAudioUrl, q.ListeningAudioTranscription,
               q.IsCodingQuestion, q.CodingTimeLimitInMinutes, q.IsCodeProjectQuestion,
               ROW_NUMBER() OVER (ORDER BY i.id) AS RowNum
        FROM AIQuestionSetItems i
        JOIN SkillChannelSampleQuestions q ON q.id = i.QuestionId
        LEFT JOIN CodeProjectDetails cpd ON cpd.QuestionId = q.id
        WHERE i.SetId = @SetId
          AND (ISNULL(q.IsCodeProjectQuestion, 0) = 0 OR cpd.ValidationStatus = 'passed')
    )
    SELECT Comment AS [Message], AIQuestionVideoLink, id,
           ISNULL(IsTypingSpeedAssessment, 0) AS IsTypingSpeedAssessment,
           ISNULL(IsListeningComprehension, 0) AS IsListeningComprehension,
           ListeningAudioUrl,
           ListeningAudioTranscription,
           ISNULL(IsCodingQuestion, 0) AS IsCodingQuestion,
           CodingTimeLimitInMinutes,
           ISNULL(IsCodeProjectQuestion, 0) AS IsCodeProjectQuestion
    FROM SetQuestions
    WHERE RowNum = @AskedCount + 1;
END
GO
