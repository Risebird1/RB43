IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerBankVerification'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SaveInterviewerBankVerification]
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveInterviewerBankVerification]
    @InterviewerProfileId   int,
    @AccountName            varchar(200),
    @AccountNumber          nvarchar(200),
    @IFSCCode               nvarchar(200),
    @IsBankVerified         bit,
    @VerifiedOn             datetime,
    @VerifiedAccountNumber  nvarchar(200),
    @VerifiedIFSCCode       nvarchar(200),
    @VerifiedAccountName    varchar(200),
    @NameAtBank             varchar(200),
    @CashfreeReferenceId    bigint
AS
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[TransInterviewerAccDetails] WHERE InterviewerProfileId = @InterviewerProfileId)
    BEGIN
        UPDATE [dbo].[TransInterviewerAccDetails]
        SET
            [AccountName]           = @AccountName,
            [AccountNumber]         = @AccountNumber,
            [IFSCCode]              = @IFSCCode,
            [IsBankVerified]        = @IsBankVerified,
            [VerifiedOn]            = @VerifiedOn,
            [VerifiedAccountNumber] = @VerifiedAccountNumber,
            [VerifiedIFSCCode]      = @VerifiedIFSCCode,
            [VerifiedAccountName]   = @VerifiedAccountName,
            [NameAtBank]            = @NameAtBank,
            [CashfreeReferenceId]   = @CashfreeReferenceId
        WHERE InterviewerProfileId = @InterviewerProfileId;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[TransInterviewerAccDetails]
        (
            [InterviewerProfileId], [AccountName], [AccountNumber], [IFSCCode],
            [IsBankVerified], [VerifiedOn], [VerifiedAccountNumber], [VerifiedIFSCCode],
            [VerifiedAccountName], [NameAtBank], [CashfreeReferenceId]
        )
        VALUES
        (
            @InterviewerProfileId, @AccountName, @AccountNumber, @IFSCCode,
            @IsBankVerified, @VerifiedOn, @VerifiedAccountNumber, @VerifiedIFSCCode,
            @VerifiedAccountName, @NameAtBank, @CashfreeReferenceId
        );
    END
END
GO
