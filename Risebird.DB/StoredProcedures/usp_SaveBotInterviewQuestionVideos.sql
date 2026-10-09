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
@CandidateAnswerAudioLink varchar(2000) = null,
-- Records the answer even though no video accompanies it.
--
-- Needed by the MCQ assessment section, where the candidate answers inside an embedded test on
-- screen.risebird.io and nothing is recorded at all - there is no spoken answer to record. Without
-- this the proc silently does nothing (not even the TimeLeftInSeconds update below), the caller
-- reads that as a failed save, and the candidate is shown a network error mid-interview.
--
-- Defaults to 0, so every existing caller keeps the old behaviour: no video means no row.
@AllowMissingVideo bit = 0,
-- Candidate transcript review. Defaulted so a caller that predates the feature leaves these alone.
@RawTranscript nvarchar(max) = null,
@TranscriptEditedByCandidate bit = null,
@TranscriptDivergencePct int = null,
@TranscriptTakeCount int = null
AS
BEGIN
    SET NOCOUNT ON;

    if(((@VideoLink is not null and @VideoLink != '') or @AllowMissingVideo = 1)
        and @CandidateResponse is not null and @CandidateResponse != '')
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
                AnswerModifiedBy = 'System',
                -- ISNULL so a re-save that does not carry them keeps what was recorded the first time.
                RawTranscript = ISNULL(@RawTranscript, RawTranscript),
                TranscriptEditedByCandidate = ISNULL(@TranscriptEditedByCandidate, TranscriptEditedByCandidate),
                TranscriptDivergencePct = ISNULL(@TranscriptDivergencePct, TranscriptDivergencePct),
                TranscriptTakeCount = ISNULL(@TranscriptTakeCount, TranscriptTakeCount)
            WHERE SessionId = @SessionId
              AND DeliveryHeadQuestionId = @DeliveryHeadQuestionId;
        END
        ELSE
        BEGIN
            -- Insert new record
            Insert into BotInterviewQuestionVideos (SessionId, VideoLink, question, CreatedAt, BotSynthesizedVideoLink, CandidateAnswer, BotAudioLink, DeliveryHeadQuestionId, CandidateAnswerAudioLink, RawTranscript, TranscriptEditedByCandidate, TranscriptDivergencePct, TranscriptTakeCount)
            values(@SessionId, @VideoLink, @question, @CreatedAt, @BotSynthesizedVideoLink, @CandidateResponse, @BotAudioLink, @DeliveryHeadQuestionId, @CandidateAnswerAudioLink, @RawTranscript, @TranscriptEditedByCandidate, @TranscriptDivergencePct, @TranscriptTakeCount)
        END

        update InterviewMeetings set TimeLeftInSeconds = @secondsLeft where id = @interviewId
    End

END
GO