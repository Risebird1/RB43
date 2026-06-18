-- Sets the single, unified Intent that both email replies and AI Plivo calls feed
-- for a NaukriOnboarding prospect.
CREATE OR ALTER PROCEDURE [dbo].[usp_SetNaukriUnifiedIntent]
    @Id           INT,
    @Intent       VARCHAR(50),
    @IntentSource VARCHAR(50),
    @ConfirmedAt  DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET Intent            = @Intent,
        IntentSource      = @IntentSource,
        IntentConfirmedAt = ISNULL(@ConfirmedAt, IntentConfirmedAt),
        UpdatedAt         = GETUTCDATE()
    WHERE Id = @Id;
END
GO
