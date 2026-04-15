CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateInterviewerEmailAvailabilityStatus]
    @Id         INT,
    @Status     VARCHAR(50),
    @ChangedBy  VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InterviewerEmailAvailability
    SET    Status     = @Status,
           ProcessedBy = ISNULL(@ChangedBy, ProcessedBy),
           UpdatedAt  = GETUTCDATE()
    WHERE  Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
