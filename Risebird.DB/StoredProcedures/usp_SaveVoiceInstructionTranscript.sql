IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveVoiceInstructionTranscript'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveVoiceInstructionTranscript
END
GO

CREATE PROCEDURE dbo.usp_SaveVoiceInstructionTranscript
    @irfid INT,
    @Transcript NVARCHAR(MAX),
    @UpdatedBy VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE IRF
    SET VoiceInstructionTranscript = @Transcript,
        VoiceInstructionTranscriptUpdatedOn = GETUTCDATE(),
        VoiceInstructionTranscriptUpdatedBy = @UpdatedBy
    WHERE id = @irfid;
END
GO
