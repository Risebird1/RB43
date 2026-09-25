IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerAgreement'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerAgreement
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Interviewer service agreement (Zoho Sign) details.
-- Looks up by profile id, or by the Zoho Sign request id when called from the webhook.
CREATE PROCEDURE dbo.usp_GetInterviewerAgreement
@InterviewerProfileId int = null,
@ZohoRequestId varchar(50) = null
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        ipr.Id AS InterviewerProfileId,
        ipr.UserId,
        ul.FirstName,
        ul.LastName,
        ul.EmailId,
        ul.MobileNumber,
        ipr.City,
        ISNULL(ipr.IsAgreementSigned, 0) AS IsAgreementSigned,
        ipr.AgreementStatus,
        ipr.AgreementVersion,
        ipr.AgreementZohoRequestId,
        ipr.AgreementZohoActionId,
        ipr.AgreementSignerName,
        ipr.AgreementFatherName,
        ipr.AgreementAddress,
        ipr.AgreementSentOn,
        ipr.AgreementSignedOn,
        ipr.AgreementCompletedOn,
        ipr.AgreementDocumentUrl,
        ipr.AgreementDeclineReason
    FROM InterviewerProfile ipr
    INNER JOIN dbo.UserList ul ON ul.id = ipr.UserId
    WHERE (@InterviewerProfileId IS NOT NULL AND ipr.Id = @InterviewerProfileId)
       OR (@InterviewerProfileId IS NULL AND @ZohoRequestId IS NOT NULL AND ipr.AgreementZohoRequestId = @ZohoRequestId)
END
GO
