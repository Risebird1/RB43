
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRandomQuestionForSession'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRandomQuestionForSession
END

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
	declare @SessionId UNIQUEIDENTIFIER = (select SessionId from InterviewMeetings where id = @interviewId)
    -- Count how many have been asked already
 --   IF (
 --       SELECT COUNT(*) 
 --       FROM TranscriptsConv 
 --       WHERE SessionId = @SessionId and [Role] = 'assistant'
 --   ) >= 25--10
 --   BEGIN
 --       --SELECT 'No more questions available.' AS Question;
	--	SELECT null;
 --       RETURN;
 --   END;

 --   ;WITH Unasked AS (
 --       SELECT q.id, q.Comment, q.AIQuestionVideoLink
 --       FROM SkillChannelSampleQuestions q
 --       WHERE q.Comment NOT IN (
 --           SELECT [Message] FROM TranscriptsConv WHERE SessionId = @SessionId and [Role] = 'assistant'
 --       ) and q.IRFId = @irfId
 --   )
 --   SELECT TOP 1 u.Comment as [Message], u.AIQuestionVideoLink, u.id
 --   FROM Unasked u
	--order by u.id --sequential pick
 --   --ORDER BY NEWID(); -- Random pick

 DECLARE @AskedCount INT = (
        SELECT COUNT(*) 
        FROM TranscriptsConv 
        WHERE SessionId = @SessionId AND [Role] = 'assistant'
    );

    -- If already asked 35 questions, stop
    IF (@AskedCount >= 35) -- change back to 10 if needed
    BEGIN
        SELECT NULL AS [Message], NULL AS AIQuestionVideoLink, NULL AS Id;
        RETURN;
    END;

    ;WITH OrderedQuestions AS (
        SELECT q.id, q.Comment, q.AIQuestionVideoLink, q.IsTypingSpeedAssessment,
               ROW_NUMBER() OVER (ORDER BY q.Id) AS RowNum
        FROM SkillChannelSampleQuestions q
        WHERE q.IRFId = @irfId
    )
    SELECT Comment AS [Message], AIQuestionVideoLink, id, 
           ISNULL(IsTypingSpeedAssessment, 0) AS IsTypingSpeedAssessment
    FROM OrderedQuestions
    WHERE RowNum = @AskedCount + 1; -- ? pick the next question




END;
