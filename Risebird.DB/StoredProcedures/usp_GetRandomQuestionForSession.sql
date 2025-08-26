
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
    IF (
        SELECT COUNT(*) 
        FROM TranscriptsConv 
        WHERE SessionId = @SessionId and [Role] = 'assistant'
    ) >= 10
    BEGIN
        --SELECT 'No more questions available.' AS Question;
		SELECT null;
        RETURN;
    END;

    ;WITH Unasked AS (
        SELECT q.id, q.Comment, q.AIQuestionVideoLink
        FROM SkillChannelSampleQuestions q
        WHERE q.Comment NOT IN (
            SELECT [Message] FROM TranscriptsConv WHERE SessionId = @SessionId and [Role] = 'assistant'
        ) and q.IRFId = @irfId
    )
    SELECT TOP 1 u.Comment as [Message], u.AIQuestionVideoLink, u.id
    FROM Unasked u
    ORDER BY NEWID(); -- Random pick
END;
