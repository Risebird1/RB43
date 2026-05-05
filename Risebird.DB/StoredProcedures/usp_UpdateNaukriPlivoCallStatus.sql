CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateNaukriPlivoCallStatus]
    @Id                        INT,
    @PlivoCallStatus           VARCHAR(50),
    @PlivoConversationId       NVARCHAR(255)  = NULL,
    @PlivoCallAt               DATETIME       = NULL,
    @PlivoCallDuration         INT            = NULL,
    @PlivoCallTranscript       NVARCHAR(MAX)  = NULL,
    @PlivoCallSummary          NVARCHAR(MAX)  = NULL,
    @PlivoProspectDisposition  VARCHAR(50)    = NULL,
    @PlivoOnboardingPreference VARCHAR(50)    = NULL,
    @PlivoKycAccepted          BIT            = NULL,
    @PlivoCalledBy             VARCHAR(150)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE NaukriProspectiveInterviewer
    SET PlivoCallStatus           = @PlivoCallStatus,
        PlivoConversationId       = ISNULL(@PlivoConversationId, PlivoConversationId),
        PlivoCallAt               = ISNULL(@PlivoCallAt, PlivoCallAt),
        PlivoCallDuration         = ISNULL(@PlivoCallDuration, PlivoCallDuration),
        PlivoCallTranscript       = ISNULL(@PlivoCallTranscript, PlivoCallTranscript),
        PlivoCallSummary          = ISNULL(@PlivoCallSummary, PlivoCallSummary),
        PlivoProspectDisposition  = ISNULL(@PlivoProspectDisposition, PlivoProspectDisposition),
        PlivoOnboardingPreference = ISNULL(@PlivoOnboardingPreference, PlivoOnboardingPreference),
        PlivoKycAccepted          = ISNULL(@PlivoKycAccepted, PlivoKycAccepted),
        PlivoCalledBy             = ISNULL(@PlivoCalledBy, PlivoCalledBy),
        UpdatedAt                 = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT;
END
GO
