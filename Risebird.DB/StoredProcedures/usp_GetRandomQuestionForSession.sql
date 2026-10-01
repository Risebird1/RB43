IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRandomQuestionForSession'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRandomQuestionForSession
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetRandomQuestionForSession
    @interviewId int,
    @irfId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SessionId UNIQUEIDENTIFIER =
        (SELECT SessionId FROM InterviewMeetings WHERE id = @interviewId);

    DECLARE @AskedCount INT = (
        SELECT COUNT(*)
        FROM TranscriptsConv
        WHERE SessionId = @SessionId AND [Role] = 'assistant'
    );

    IF (@AskedCount >= 35)
    BEGIN
        SELECT NULL AS [Message], NULL AS AIQuestionVideoLink, NULL AS Id;
        RETURN;
    END;

    ;WITH OrderedQuestions AS (
        SELECT q.id, q.Comment, q.AIQuestionVideoLink, q.IsTypingSpeedAssessment,
               q.IsListeningComprehension, q.ListeningAudioUrl, q.ListeningAudioTranscription,
               q.IsCodingQuestion, q.CodingTimeLimitInMinutes, q.IsCodeProjectQuestion,
               ROW_NUMBER() OVER (ORDER BY q.Id) AS RowNum
        FROM SkillChannelSampleQuestions q
        LEFT JOIN CodeProjectDetails cpd ON cpd.QuestionId = q.id
        WHERE q.IRFId = @irfId
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
    FROM OrderedQuestions
    WHERE RowNum = @AskedCount + 1;
END
GO
