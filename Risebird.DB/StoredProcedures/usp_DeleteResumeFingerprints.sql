IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteResumeFingerprints'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteResumeFingerprints
END
GO

-- Removes a candidate's fingerprints (bands cascade) before they are re-computed.
Create PROCEDURE [dbo].[usp_DeleteResumeFingerprints]
@CandidateId int
AS
Begin
	SET NOCOUNT ON;
	delete from ResumeFingerprint where CandidateId = @CandidateId
End
GO

