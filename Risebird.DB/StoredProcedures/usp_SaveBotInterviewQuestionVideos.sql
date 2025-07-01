IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveBotInterviewQuestionVideos'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveBotInterviewQuestionVideos
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_SaveBotInterviewQuestionVideos
@VideoLink varchar(1000),
@question varchar(1000),
@SessionId varchar(50),
@CreatedAt datetime
AS
BEGIN
Insert into BotInterviewQuestionVideos (SessionId,VideoLink,question,CreatedAt) values(@SessionId,@VideoLink,@question,@CreatedAt)
END
GO