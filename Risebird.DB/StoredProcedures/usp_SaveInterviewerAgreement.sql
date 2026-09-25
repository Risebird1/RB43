IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerAgreement'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerAgreement
END

-- Replaced by usp_SaveInterviewerAgreement
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerAgreementRequest'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerAgreementRequest
END

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInterviewerAgreementStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerAgreementStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Saves the interviewer service agreement (Zoho Sign) on InterviewerProfile.
--
-- New request   (@InterviewerProfileId passed): records the newly created Zoho Sign request and
--               resets the outcome fields of any previous (declined / expired / recalled) request.
--               Never touches a signed agreement.
-- Status update (@InterviewerProfileId null): syncs the status by @ZohoRequestId (webhook /
--               return-from-signing), so a late event for an older request never touches a newer
--               one. A completed agreement is never downgraded, but can be re-synced (e.g. to fill
--               in the signed document URL if storing the PDF failed the first time).
--               Name / father's name / address are the values the interviewer filled in on the
--               agreement inside Zoho Sign, read back after signing.
--
-- Returns the number of rows updated.
CREATE PROCEDURE dbo.usp_SaveInterviewerAgreement
@InterviewerProfileId int = null,
@ZohoRequestId varchar(50),
@AgreementStatus varchar(30),
-- new request
@ZohoActionId varchar(50) = null,
@AgreementVersion varchar(20) = null,
-- status update
@IsAgreementSigned bit = 0,
@SignedOn datetime = null,
@CompletedOn datetime = null,
@DocumentUrl varchar(500) = null,
@DeclineReason nvarchar(500) = null,
@SignerName nvarchar(150) = null,
@FatherName nvarchar(150) = null,
@Address nvarchar(500) = null
AS
BEGIN
    SET NOCOUNT ON;

    IF @InterviewerProfileId IS NOT NULL
    BEGIN
        UPDATE InterviewerProfile
        SET AgreementZohoRequestId = @ZohoRequestId,
            AgreementZohoActionId  = @ZohoActionId,
            AgreementStatus        = @AgreementStatus,
            AgreementVersion       = @AgreementVersion,
            AgreementSignerName    = NULL,
            AgreementFatherName    = NULL,
            AgreementAddress       = NULL,
            AgreementSentOn        = DATEADD(MINUTE, 330, GETUTCDATE()),
            AgreementSignedOn      = NULL,
            AgreementCompletedOn   = NULL,
            AgreementDocumentUrl   = NULL,
            AgreementDeclineReason = NULL,
            IsAgreementSigned      = 0
        WHERE Id = @InterviewerProfileId
          AND ISNULL(IsAgreementSigned, 0) = 0
    END
    ELSE
    BEGIN
        UPDATE InterviewerProfile
        SET AgreementStatus        = @AgreementStatus,
            IsAgreementSigned      = @IsAgreementSigned,
            AgreementSignedOn      = ISNULL(@SignedOn, AgreementSignedOn),
            AgreementCompletedOn   = ISNULL(@CompletedOn, AgreementCompletedOn),
            AgreementDocumentUrl   = ISNULL(@DocumentUrl, AgreementDocumentUrl),
            AgreementDeclineReason = ISNULL(@DeclineReason, AgreementDeclineReason),
            AgreementSignerName    = ISNULL(@SignerName, AgreementSignerName),
            AgreementFatherName    = ISNULL(@FatherName, AgreementFatherName),
            AgreementAddress       = ISNULL(@Address, AgreementAddress)
        WHERE AgreementZohoRequestId = @ZohoRequestId
          AND (ISNULL(IsAgreementSigned, 0) = 0 OR @IsAgreementSigned = 1)
    END

    SELECT @@ROWCOUNT
END
GO
