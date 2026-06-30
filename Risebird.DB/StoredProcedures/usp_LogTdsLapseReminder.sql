CREATE OR ALTER PROCEDURE [dbo].[usp_LogTdsLapseReminder]
    @Userid            INT,
    @PaymentReference  NVARCHAR(100),
    @ReminderIndex     INT,
    @EmailTo           NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM TdsLapseReminderLog
        WHERE Userid = @Userid
          AND PaymentReference = @PaymentReference
          AND ReminderIndex = @ReminderIndex
    )
    BEGIN
        INSERT INTO TdsLapseReminderLog (Userid, PaymentReference, ReminderIndex, SentOn, EmailTo)
        VALUES (@Userid, @PaymentReference, @ReminderIndex, GETDATE(), @EmailTo);
        SELECT 1 AS Inserted;
    END
    ELSE
    BEGIN
        SELECT 0 AS Inserted;
    END
END
GO
