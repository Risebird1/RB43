CREATE OR ALTER PROCEDURE [dbo].[usp_SetIAUnifiedIntent]
    @Id           INT,
    @Intent       VARCHAR(50),
    @IntentSource VARCHAR(20),
    @ConfirmedAt  DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Single canonical availability intent fed by both the email reply and the
    -- Plivo voice-agent call. Canonical values: 'Available' | 'NotAvailable' | 'Unsubscribe'.
    UPDATE InterviewerEmailAvailability
    SET Intent            = @Intent,
        IntentSource      = @IntentSource,
        IntentConfirmedAt = ISNULL(@ConfirmedAt, IntentConfirmedAt),
        UpdatedAt         = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
