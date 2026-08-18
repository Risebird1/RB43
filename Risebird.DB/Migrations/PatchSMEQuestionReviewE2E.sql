-- End-to-end SME review serving + draft stamp + evaluator expected-answer parts.
-- Run after AddSMEQuestionReview.sql.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.usp_GetRandomQuestionForSession', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetRandomQuestionForSession;
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

IF OBJECT_ID(N'dbo.usp_AddAIGeneratedSampleQuestion', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_AddAIGeneratedSampleQuestion;
GO

CREATE PROCEDURE dbo.usp_AddAIGeneratedSampleQuestion
    @IRFId int,
    @SkillName nvarchar(500),
    @Comment nvarchar(max),
    @ExpectedAnswer nvarchar(max),
    @GeneratedPayload nvarchar(max),
    @CreatedBy varchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO SkillChannelSampleQuestions
        (IRFId, Skill, SkillName, Comment, ExpectedAnswer, GeneratedPayload, IsGeneratedByAi, CreatedBy, VettingStatus)
    VALUES
        (@IRFId, 0, @SkillName, @Comment, @ExpectedAnswer, @GeneratedPayload, 1, @CreatedBy, 0)

    SELECT SCOPE_IDENTITY() AS NewId
END
GO

IF OBJECT_ID(N'dbo.usp_GetAIInterviewDetailsBySessionId', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetAIInterviewDetailsBySessionId;
GO

CREATE PROCEDURE dbo.usp_GetAIInterviewDetailsBySessionId
    @SessionId varchar(50)
AS
BEGIN
    DECLARE @irfId int, @aiInterviewType int, @biqvId int
    SET @irfId = (SELECT id FROM IRF WHERE IRFId = (SELECT IRFId FROM InterviewMeetings WHERE SessionId = @SessionId))
    SET @biqvId = (SELECT TOP 1 biqvId FROM VideoSentimentAnalysis WHERE biqvId IN (SELECT id FROM BotInterviewQuestionVideos WHERE SessionId = @SessionId))
    SET @aiInterviewType = (SELECT AIInterviewType FROM IRF WHERE id = @irfId)

    SELECT im.id AS InterviewId, im.interviewDate, im.AssistantId, im.candidateEmail, ulc.MobileNumber, im.interviewer,
           im.TimeLeftInSeconds, im.TimeZonesValue, im.TimeZoneoffset, irf.JDTiltle, irf.JDLink, iss.ThreadId, ca.ResumeLink AS resumeLink,
           ISNULL(ad.matchPercentage, 0) AS MatchScorePercentage,
           ISNULL(ad.id, 0) AS AadharVerificationId, ic.MeetingStatus, ic.InterviewStatus,
           CASE WHEN (im.candidateName IS NOT NULL AND im.candidateName != '') THEN im.candidateName ELSE ulc.FirstName END AS candidateName,
           irf.id AS irfIntId, irf.AIInterviewType, IRF.IsAIInterview, irf.OpenAIAssistantId, irf.showChatBox, irf.isMultiLingual,
           CASE WHEN irf.AIInterviewType = 1 THEN 1 ELSE 0 END AS IsAdaptive, @biqvId AS CandidateAnswerVideoIdAI,
           ca.AIResumeScreeningVerdict, ca.AIResumeScreeningScore
    FROM InterviewMeetings im
    LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
    LEFT JOIN AadhaarDetails ad ON (ad.email = im.candidateEmail AND ad.reference_id = CAST(im.id AS varchar(50)))
    JOIN IRF ON IRF.irfId = im.irfid
    JOIN UserList ulc ON ulc.EmailId = im.candidateEmail
    JOIN CandidatesMaster cm ON cm.UserId = ulc.id
    JOIN Candidate ca ON (ca.CandidateMasterId = cm.id AND ca.IRFId = irf.id)
    OUTER APPLY (SELECT ThreadId FROM InterviewSessions WHERE SessionId = @SessionId) iss
    WHERE im.SessionId = @SessionId

    SELECT * FROM TransTechSkill WHERE IRFId = @irfId

    IF (@aiInterviewType = 2)
    BEGIN
        SELECT bq.question, bq.CandidateAnswer,
               CASE
                   WHEN NULLIF(LTRIM(RTRIM(CONCAT(
                        ISNULL(sq.ExpectedOutput, ''),
                        ISNULL(sq.AnswerExplanation, ''),
                        ISNULL(sq.SafeImplementation, ''),
                        ISNULL(sq.SemanticMatchCriteria, ''),
                        ISNULL(sq.MisconceptionRedFlags, '')
                   ))), '') IS NOT NULL
                   THEN CONCAT(
                        CASE WHEN NULLIF(LTRIM(RTRIM(sq.ExpectedOutput)), '') IS NOT NULL
                             THEN N'Expected output:' + CHAR(13) + CHAR(10) + sq.ExpectedOutput + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)
                             ELSE N'' END,
                        CASE WHEN NULLIF(LTRIM(RTRIM(sq.AnswerExplanation)), '') IS NOT NULL
                             THEN N'Explanation:' + CHAR(13) + CHAR(10) + sq.AnswerExplanation + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)
                             ELSE N'' END,
                        CASE WHEN NULLIF(LTRIM(RTRIM(sq.SafeImplementation)), '') IS NOT NULL
                             THEN N'Safe implementation:' + CHAR(13) + CHAR(10) + sq.SafeImplementation + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)
                             ELSE N'' END,
                        CASE WHEN NULLIF(LTRIM(RTRIM(sq.SemanticMatchCriteria)), '') IS NOT NULL
                             THEN N'Semantic matching criteria:' + CHAR(13) + CHAR(10) + sq.SemanticMatchCriteria + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)
                             ELSE N'' END,
                        CASE WHEN NULLIF(LTRIM(RTRIM(sq.MisconceptionRedFlags)), '') IS NOT NULL
                             THEN N'Misconception red flags:' + CHAR(13) + CHAR(10) + sq.MisconceptionRedFlags
                             ELSE N'' END
                   )
                   ELSE sq.ExpectedAnswer
               END AS ExpectedAnswer,
               sq.ExpectedOutput, sq.AnswerExplanation, sq.SafeImplementation,
               sq.SemanticMatchCriteria, sq.MisconceptionRedFlags,
               ISNULL(sq.MaxScore, 0) AS MaxScore,
               sq.SkillName
        FROM BotInterviewQuestionVideos bq
        JOIN SkillChannelSampleQuestions sq ON bq.DeliveryHeadQuestionId = sq.id
        WHERE irfid = @irfId AND SessionId = @SessionId
    END
END
GO
