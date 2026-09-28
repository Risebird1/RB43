IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerAgreement'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerAgreement
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Interviewer service agreement details of one interviewer.
CREATE PROCEDURE dbo.usp_GetInterviewerAgreement
@InterviewerProfileId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id AS InterviewerProfileId,
        ISNULL(IsAgreementSigned, 0) AS IsAgreementSigned,
        AgreementVersion,
        AgreementSignerName,
        AgreementFatherName,
        AgreementAddress,
        AgreementSignedOn,
        AgreementSignatureUrl,
        AgreementProofUrl
    FROM InterviewerProfile
    WHERE Id = @InterviewerProfileId
END
GO
