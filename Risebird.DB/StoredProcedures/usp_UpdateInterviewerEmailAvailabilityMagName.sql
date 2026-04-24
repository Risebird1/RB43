CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateInterviewerEmailAvailabilityMagName]
    @Id         INT,
    @MagName    NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE InterviewerEmailAvailability
    SET MagName   = @MagName,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
