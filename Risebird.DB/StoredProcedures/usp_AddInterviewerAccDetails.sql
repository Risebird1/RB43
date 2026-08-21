/****** Object:  StoredProcedure [dbo].[usp_AddInterviewerAccDetails]    Script Date: 5/1/2020 4:02:17 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewerAccDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewerAccDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddInterviewerAccDetails]
    @InterviewerProfileId   int,
    @AccountName            varchar(200),
    @AccountNumber          nvarchar(200),
    @IFSCCode               nvarchar(200),
    @UPIId                  nvarchar(250),
    @PAN                    nvarchar(200),
    @IsBankVerified         bit = 0,
    @VerifiedOn             datetime = null,
    @VerifiedAccountNumber  nvarchar(200) = null,
    @VerifiedIFSCCode       nvarchar(200) = null,
    @VerifiedAccountName    varchar(200) = null,
    @NameAtBank             varchar(200) = null,
    @CashfreeReferenceId    bigint = null,
    @IsPanVerified              bit = 0,
    @PanVerifiedOn              datetime = null,
    @VerifiedPAN                nvarchar(20) = null,
    @NameAtPan                  varchar(200) = null,
    @PanVerificationReferenceId bigint = null
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM [dbo].[TransInterviewerAccDetails]
        WHERE InterviewerProfileId = @InterviewerProfileId)
    BEGIN
        UPDATE [dbo].[TransInterviewerAccDetails]
        SET
            [AccountName]           = @AccountName,
            [AccountNumber]         = @AccountNumber,
            [IFSCCode]              = @IFSCCode,
            [UPIId]                 = @UPIId,
            [PAN]                   = @PAN,
            [IsBankVerified]        = @IsBankVerified,
            [VerifiedOn]            = @VerifiedOn,
            [VerifiedAccountNumber] = @VerifiedAccountNumber,
            [VerifiedIFSCCode]      = @VerifiedIFSCCode,
            [VerifiedAccountName]   = @VerifiedAccountName,
            [NameAtBank]            = @NameAtBank,
            [CashfreeReferenceId]   = @CashfreeReferenceId,
            [IsPanVerified]              = @IsPanVerified,
            [PanVerifiedOn]              = @PanVerifiedOn,
            [VerifiedPAN]                = @VerifiedPAN,
            [NameAtPan]                  = @NameAtPan,
            [PanVerificationReferenceId] = @PanVerificationReferenceId
        WHERE InterviewerProfileId = @InterviewerProfileId;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[TransInterviewerAccDetails]
        (
            [InterviewerProfileId],
            [AccountName],
            [AccountNumber],
            [IFSCCode],
            [UPIId],
            [PAN],
            [IsBankVerified],
            [VerifiedOn],
            [VerifiedAccountNumber],
            [VerifiedIFSCCode],
            [VerifiedAccountName],
            [NameAtBank],
            [CashfreeReferenceId],
            [IsPanVerified],
            [PanVerifiedOn],
            [VerifiedPAN],
            [NameAtPan],
            [PanVerificationReferenceId]
        )
        VALUES
        (
            @InterviewerProfileId,
            @AccountName,
            @AccountNumber,
            @IFSCCode,
            @UPIId,
            @PAN,
            @IsBankVerified,
            @VerifiedOn,
            @VerifiedAccountNumber,
            @VerifiedIFSCCode,
            @VerifiedAccountName,
            @NameAtBank,
            @CashfreeReferenceId,
            @IsPanVerified,
            @PanVerifiedOn,
            @VerifiedPAN,
            @NameAtPan,
            @PanVerificationReferenceId
        );
    END
END
GO
