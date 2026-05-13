CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateProspectAutomationStatus]
    @Id                      INT,
    @AutomationStatus        VARCHAR(50),
    @IncrementCallAttempt    BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);

    UPDATE NaukriProspectiveInterviewer
    SET AutomationStatus          = @AutomationStatus,
        AutomationLastProcessedAt = GETUTCDATE(),
        PlivoCallAttemptsTotal    = CASE WHEN @IncrementCallAttempt = 1
                                         THEN ISNULL(PlivoCallAttemptsTotal, 0) + 1
                                         ELSE PlivoCallAttemptsTotal END,
        PlivoCallAttemptsToday    = CASE WHEN @IncrementCallAttempt = 1
                                         THEN CASE WHEN ISNULL(PlivoCallLastAttemptDate, '1900-01-01') < @Today
                                                    THEN 1
                                                    ELSE ISNULL(PlivoCallAttemptsToday, 0) + 1
                                              END
                                         ELSE PlivoCallAttemptsToday END,
        PlivoCallLastAttemptDate  = CASE WHEN @IncrementCallAttempt = 1
                                         THEN @Today
                                         ELSE PlivoCallLastAttemptDate END,
        UpdatedAt                 = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT;
END
GO
