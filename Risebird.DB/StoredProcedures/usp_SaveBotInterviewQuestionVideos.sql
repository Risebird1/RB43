IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveBotInterviewQuestionVideos'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveBotInterviewQuestionVideos
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Prerequisite: run Migrations/AddCandidateAnswerAudioLinkToBotInterviewQuestionVideos.sql first.
Create Procedure usp_SaveBotInterviewQuestionVideos
@VideoLink varchar(1000),
@question nvarchar(max),
@SessionId varchar(50),
@CreatedAt datetime,
@secondsLeft int,
@interviewId int,
@BotSynthesizedVideoLink varchar(2000) = null,
@CandidateResponse nvarchar(max),
@BotAudioLink varchar(2000) = null,
@DeliveryHeadQuestionId int = null,
@CandidateAnswerAudioLink varchar(2000) = null
AS
BEGIN
    SET NOCOUNT ON;

    if(@VideoLink is not null and @VideoLink != '' and @CandidateResponse is not null and @CandidateResponse != '')
    Begin
        -- Check if a record already exists for this SessionId + DeliveryHeadQuestionId combination
        IF EXISTS (
            SELECT 1 
            FROM BotInterviewQuestionVideos 
            WHERE SessionId = @SessionId 
              AND DeliveryHeadQuestionId = @DeliveryHeadQuestionId
              AND DeliveryHeadQuestionId IS NOT NULL
        )
        BEGIN
            -- Update existing record
            UPDATE BotInterviewQuestionVideos
            SET 
                VideoLink = @VideoLink,
                question = @question,
                BotSynthesizedVideoLink = @BotSynthesizedVideoLink,
                CandidateAnswer = @CandidateResponse,
                BotAudioLink = @BotAudioLink,
                CandidateAnswerAudioLink = @CandidateAnswerAudioLink,
                AnswerModifiedAt = GETDATE(),
                AnswerModifiedBy = 'System'
            WHERE SessionId = @SessionId 
              AND DeliveryHeadQuestionId = @DeliveryHeadQuestionId;
        END
        ELSE
        BEGIN
            -- Insert new record
            Insert into BotInterviewQuestionVideos (SessionId, VideoLink, question, CreatedAt, BotSynthesizedVideoLink, CandidateAnswer, BotAudioLink, DeliveryHeadQuestionId, CandidateAnswerAudioLink) 
            values(@SessionId, @VideoLink, @question, @CreatedAt, @BotSynthesizedVideoLink, @CandidateResponse, @BotAudioLink, @DeliveryHeadQuestionId, @CandidateAnswerAudioLink)
        END

        update InterviewMeetings set TimeLeftInSeconds = @secondsLeft where id = @interviewId
    End

END
GO