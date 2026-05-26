CREATE OR ALTER PROCEDURE [dbo].[usp_GetIABatchCallHistory]
    @MagName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, MagName, InitiatedBy, InitiatedAt,
           InterviewersAttempted, InterviewersSucceeded,
           InterviewersFailed, InterviewersSkipped,
           CompletedAt, Status
    FROM IABatchCallLog
    WHERE MagName = @MagName
    ORDER BY Id DESC;
END
GO
