IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SetSampleQuestionListeningMode'))
BEGIN
    DROP PROCEDURE dbo.usp_SetSampleQuestionListeningMode
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Toggles listening comprehension mode on an existing question without touching its text,
-- mirroring usp_SetSampleQuestionCodingMode. The audio url is set separately by
-- usp_UpdateSampleQuestionListeningAudio, because the file is uploaded on its own request.
CREATE PROCEDURE [dbo].[usp_SetSampleQuestionListeningMode]
    @id int,
    @isListeningComprehension bit = 0,
    @listeningAudioTranscription nvarchar(max) = NULL,
    @questionGist nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE SkillChannelSampleQuestions
    SET IsListeningComprehension = ISNULL(@isListeningComprehension, 0),
        -- Transcript and gist are prose the user typed. They survive un-ticking the flag so
        -- re-ticking it does not force them to be written again, and so the audio url that
        -- pairs with the transcript is never left describing a different clip.
        ListeningAudioTranscription = ISNULL(@listeningAudioTranscription, ListeningAudioTranscription),
        QuestionGist = ISNULL(@questionGist, QuestionGist)
    WHERE id = @id;
END
GO
