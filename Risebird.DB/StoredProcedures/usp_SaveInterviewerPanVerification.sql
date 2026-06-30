IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerPanVerification'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SaveInterviewerPanVerification]
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveInterviewerPanVerification]
    @InterviewerProfileId       int,
    @PAN                        nvarchar(20),
    @IsPanVerified              bit,
    @PanVerifiedOn              datetime,
    @VerifiedPAN                nvarchar(20),
    @NameAtPan                  varchar(200),
    @PanVerificationReferenceId bigint
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM [dbo].[TransInterviewerAccDetails] WHERE InterviewerProfileId = @InterviewerProfileId)
    BEGIN
        UPDATE [dbo].[TransInterviewerAccDetails]
        SET
            [PAN]                        = @PAN,
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
            [PAN],
            [IsPanVerified],
            [PanVerifiedOn],
            [VerifiedPAN],
            [NameAtPan],
            [PanVerificationReferenceId]
        )
        VALUES
        (
            @InterviewerProfileId,
            @PAN,
            @IsPanVerified,
            @PanVerifiedOn,
            @VerifiedPAN,
            @NameAtPan,
            @PanVerificationReferenceId
        );
    END
END
GO
