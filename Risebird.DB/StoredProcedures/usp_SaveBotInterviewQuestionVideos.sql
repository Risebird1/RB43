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
@CreatedAt datetime,
@secondsLeft int,
@interviewId int,
@BotSynthesizedVideoLink varchar(2000) = null,
@CandidateResponse varchar(2000),
@BotAudioLink varchar(2000) = null,
@DeliveryHeadQuestionId int = null
AS
BEGIN

Insert into BotInterviewQuestionVideos (SessionId, VideoLink, question, CreatedAt, BotSynthesizedVideoLink, CandidateAnswer, BotAudioLink, DeliveryHeadQuestionId) 
values(@SessionId, @VideoLink, @question, @CreatedAt, @BotSynthesizedVideoLink, @CandidateResponse, @BotAudioLink, @DeliveryHeadQuestionId)

update InterviewMeetings set TimeLeftInSeconds = @secondsLeft where id = @interviewId

END
GO