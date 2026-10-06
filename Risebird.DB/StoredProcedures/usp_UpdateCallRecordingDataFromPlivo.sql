/*
================================================================================
usp_UpdateCallRecordingDataFromPlivo
Writes Plivo callback data (PlivoVoice/DialCallback) onto the CallRecordingData
row that User/makecall already inserted for the browser call (CallSid = Plivo
A-leg CallUUID). Never inserts: an unknown CallSid updates nothing.

  Duration     seconds; only raised, never lowered (Dial and Record callbacks
               can both report it)
  StartTime    EndTime - Duration, only when not already set
  EndTime      UTC
  Status       e.g. completed / busy / no-answer
  HangupCause  e.g. NORMAL_CLEARING / USER_BUSY / NO_ANSWER
  RecordingUri only filled when empty

Requires the HangupCause / EndTime columns from Tables/CallRecordingData.sql.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateCallRecordingDataFromPlivo]
    @CallSid         VARCHAR(50),
    @DurationSeconds INT           = NULL,
    @EndTimeUtc      DATETIME      = NULL,
    @Status          VARCHAR(50)   = NULL,
    @HangupCause     VARCHAR(100)  = NULL,
    @RecordingUri    VARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @CallSid = NULLIF(LTRIM(RTRIM(@CallSid)), '');
    IF @CallSid IS NULL
    BEGIN
        SELECT 0 AS RowsUpdated;
        RETURN;
    END

    IF @DurationSeconds < 0
        SET @DurationSeconds = NULL;

    SET @Status       = NULLIF(LTRIM(RTRIM(@Status)), '');
    SET @HangupCause  = NULLIF(LTRIM(RTRIM(@HangupCause)), '');
    SET @RecordingUri = NULLIF(LTRIM(RTRIM(@RecordingUri)), '');

    UPDATE dbo.CallRecordingData
    SET Duration = CASE
            WHEN @DurationSeconds IS NOT NULL
             AND (TRY_CAST(Duration AS INT) IS NULL OR TRY_CAST(Duration AS INT) < @DurationSeconds)
                THEN CAST(@DurationSeconds AS VARCHAR(10))
            ELSE Duration END,
        StartTime = CASE
            WHEN StartTime IS NULL AND @EndTimeUtc IS NOT NULL AND @DurationSeconds IS NOT NULL
                THEN DATEADD(SECOND, -@DurationSeconds, @EndTimeUtc)
            ELSE StartTime END,
        EndTime      = COALESCE(@EndTimeUtc, EndTime),
        [Status]     = COALESCE(LEFT(@Status, 20), [Status]),
        HangupCause  = COALESCE(@HangupCause, HangupCause),
        RecordingUri = CASE
            WHEN NULLIF(LTRIM(RTRIM(RecordingUri)), '') IS NULL THEN @RecordingUri
            ELSE RecordingUri END
    WHERE CallSid = @CallSid;

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO
