CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateIAEmailAvailabilityPlivoCallStatus]
    @Id                    INT,
    @PlivoCallStatus       VARCHAR(50),
    @PlivoConversationId   VARCHAR(200) = NULL,
    @PlivoCallAt           DATETIME = NULL,
    @PlivoCallDuration     INT = NULL,
    @PlivoCallTranscript   NVARCHAR(MAX) = NULL,
    @PlivoCallSummary      NVARCHAR(MAX) = NULL,
    @PlivoIntent           VARCHAR(50) = NULL,
    @PlivoAssignmentHelp   BIT = NULL,
    @PlivoEmailReceived    BIT = NULL,
    @PlivoCallbackRequested BIT = NULL,
    @PlivoNotes            NVARCHAR(500) = NULL,
    @PlivoCalledBy         VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE InterviewerEmailAvailability
    SET PlivoCallStatus       = ISNULL(@PlivoCallStatus, PlivoCallStatus),
        PlivoConversationId   = ISNULL(@PlivoConversationId, PlivoConversationId),
        PlivoCallAt           = ISNULL(@PlivoCallAt, PlivoCallAt),
        PlivoCallDuration     = ISNULL(@PlivoCallDuration, PlivoCallDuration),
        PlivoCallTranscript   = ISNULL(@PlivoCallTranscript, PlivoCallTranscript),
        PlivoCallSummary      = ISNULL(@PlivoCallSummary, PlivoCallSummary),
        PlivoIntent           = ISNULL(@PlivoIntent, PlivoIntent),
        PlivoAssignmentHelp   = ISNULL(@PlivoAssignmentHelp, PlivoAssignmentHelp),
        PlivoEmailReceived    = ISNULL(@PlivoEmailReceived, PlivoEmailReceived),
        PlivoCallbackRequested = ISNULL(@PlivoCallbackRequested, PlivoCallbackRequested),
        PlivoNotes            = ISNULL(@PlivoNotes, PlivoNotes),
        PlivoCalledBy         = ISNULL(@PlivoCalledBy, PlivoCalledBy),
        UpdatedAt             = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
