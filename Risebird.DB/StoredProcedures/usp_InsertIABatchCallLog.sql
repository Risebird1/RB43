CREATE OR ALTER PROCEDURE [dbo].[usp_InsertIABatchCallLog]
    @MagName           NVARCHAR(200),
    @InitiatedBy       VARCHAR(100),
    @TotalInterviewers INT,
    @CampaignId        INT = NULL,
    @NewId             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO IABatchCallLog (MagName, InitiatedBy, InterviewersAttempted, CampaignId)
    VALUES (@MagName, @InitiatedBy, @TotalInterviewers, @CampaignId);

    SET @NewId = SCOPE_IDENTITY();
END
GO
