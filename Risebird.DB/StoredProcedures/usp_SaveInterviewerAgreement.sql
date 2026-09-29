IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerAgreement'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerAgreement
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Saves the interviewer's signed service agreement: details, signature image and the proof screenshot of the
-- signing page. An agreement that is already signed is not changed.
-- Returns the number of rows updated (0 = not found or already signed).
CREATE PROCEDURE dbo.usp_SaveInterviewerAgreement
@InterviewerProfileId int,
@AgreementVersion varchar(20),
@SignerName nvarchar(150),
@FatherName nvarchar(150),
@Address nvarchar(500),
@SignatureUrl varchar(500),
@ProofUrl varchar(500)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE InterviewerProfile
    SET IsAgreementSigned     = 1,
        AgreementVersion      = @AgreementVersion,
        AgreementSignerName   = @SignerName,
        AgreementFatherName   = @FatherName,
        AgreementAddress      = @Address,
        AgreementSignatureUrl = @SignatureUrl,
        AgreementProofUrl     = @ProofUrl,
        AgreementSignedOn     = DATEADD(MINUTE, 330, GETUTCDATE())
    WHERE Id = @InterviewerProfileId
      AND ISNULL(IsAgreementSigned, 0) = 0

    SELECT @@ROWCOUNT
END
GO
