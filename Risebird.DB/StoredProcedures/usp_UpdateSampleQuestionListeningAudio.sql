IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateSampleQuestionListeningAudio'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateSampleQuestionListeningAudio
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Stores the uploaded listening audio clip's url against a question. Called after the file
-- has landed in blob storage, so the url is already public/SAS-signed by the caller.
CREATE PROCEDURE [dbo].[usp_UpdateSampleQuestionListeningAudio]
    @id int,
    @listeningAudioUrl varchar(max)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE SkillChannelSampleQuestions
    SET ListeningAudioUrl = @listeningAudioUrl
    WHERE id = @id;
END
GO
