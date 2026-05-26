CREATE OR ALTER PROCEDURE [dbo].[usp_InsertIABatchCallLog]
    @MagName           NVARCHAR(200),
    @InitiatedBy       VARCHAR(100),
    @TotalInterviewers INT,
    @NewId             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO IABatchCallLog (MagName, InitiatedBy, InterviewersAttempted)
    VALUES (@MagName, @InitiatedBy, @TotalInterviewers);

    SET @NewId = SCOPE_IDENTITY();
END
GO
