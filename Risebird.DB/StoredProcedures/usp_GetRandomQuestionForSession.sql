
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRandomQuestionForSession'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRandomQuestionForSession
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE dbo.usp_GetRandomQuestionForSession
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

    DECLARE @SetId int = NULL;
    SELECT @SetId = m.QuestionSetId
    FROM InterviewMeetings m
    WHERE m.id = @interviewId;

    IF (@SetId IS NULL OR NOT EXISTS (
            SELECT 1 FROM dbo.AICodeInterviewQuestionSet s
            WHERE s.Id = @SetId AND s.IsDeleted = 0 AND s.ReviewStatus = 1
       ))
    BEGIN
        SELECT TOP 1 @SetId = s.Id
        FROM dbo.AICodeInterviewQuestionSet s
        WHERE s.IRFId = @irfId AND s.IsDeleted = 0 AND s.ReviewStatus = 1
        ORDER BY (
            SELECT COUNT(*) FROM dbo.InterviewMeetings m WHERE m.QuestionSetId = s.Id
        ), s.Id;

        IF @SetId IS NOT NULL
            UPDATE dbo.InterviewMeetings
            SET QuestionSetId = @SetId
            WHERE id = @interviewId
              AND (QuestionSetId IS NULL OR QuestionSetId <> @SetId);
    END

    IF @SetId IS NOT NULL
       AND EXISTS (
            SELECT 1 FROM dbo.AICodeInterviewQuestionSet s
            WHERE s.Id = @SetId AND s.IsDeleted = 0 AND s.ReviewStatus = 1
       )
    BEGIN
        ;WITH SetQs AS (
            SELECT q.id, q.Comment, q.AIQuestionVideoLink, q.IsTypingSpeedAssessment,
                   q.IsListeningComprehension, q.ListeningAudioUrl, q.ListeningAudioTranscription,
                   q.IsCodingQuestion, q.CodingTimeLimitInMinutes,
                   ROW_NUMBER() OVER (ORDER BY i.DisplayOrder, q.Id) AS RowNum
            FROM dbo.AICodeInterviewQuestionSetItem i
            INNER JOIN dbo.SkillChannelSampleQuestions q ON q.id = i.QuestionBankId
            WHERE i.QuestionSetId = @SetId
              AND (q.VettingStatus IS NULL OR q.VettingStatus = 1)
        )
        SELECT Comment AS [Message], AIQuestionVideoLink, id,
               ISNULL(IsTypingSpeedAssessment, 0) AS IsTypingSpeedAssessment,
               ISNULL(IsListeningComprehension, 0) AS IsListeningComprehension,
               ListeningAudioUrl,
               ListeningAudioTranscription,
               ISNULL(IsCodingQuestion, 0) AS IsCodingQuestion,
               CodingTimeLimitInMinutes
        FROM SetQs
        WHERE RowNum = @AskedCount + 1;
        RETURN;
    END

    ;WITH OrderedQuestions AS (
        SELECT q.id, q.Comment, q.AIQuestionVideoLink, q.IsTypingSpeedAssessment,
               q.IsListeningComprehension, q.ListeningAudioUrl, q.ListeningAudioTranscription,
               q.IsCodingQuestion, q.CodingTimeLimitInMinutes,
               ROW_NUMBER() OVER (ORDER BY q.Id) AS RowNum
        FROM dbo.SkillChannelSampleQuestions q
        WHERE q.IRFId = @irfId
          AND (q.VettingStatus IS NULL OR q.VettingStatus = 1)
          AND (
                q.QuestionSlot IS NULL
                OR LTRIM(RTRIM(q.QuestionSlot)) = ''
                OR NOT EXISTS (
                    SELECT 1
                    FROM TranscriptsConv t
                    INNER JOIN dbo.SkillChannelSampleQuestions aq ON aq.id = t.DeliveryHeadQuestionId
                    WHERE t.SessionId = @SessionId
                      AND t.[Role] = 'assistant'
                      AND aq.QuestionSlot = q.QuestionSlot
                      AND NULLIF(LTRIM(RTRIM(aq.QuestionSlot)), '') IS NOT NULL
                )
              )
    )
    SELECT Comment AS [Message], AIQuestionVideoLink, id,
           ISNULL(IsTypingSpeedAssessment, 0) AS IsTypingSpeedAssessment,
           ISNULL(IsListeningComprehension, 0) AS IsListeningComprehension,
           ListeningAudioUrl,
           ListeningAudioTranscription,
           ISNULL(IsCodingQuestion, 0) AS IsCodingQuestion,
           CodingTimeLimitInMinutes
    FROM OrderedQuestions
    WHERE RowNum = @AskedCount + 1;
END
GO
