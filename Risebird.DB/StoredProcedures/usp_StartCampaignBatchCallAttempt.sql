-- Opens a new batch-call ATTEMPT for a campaign and returns its identifiers.
--
-- Multiple attempts per campaign are allowed. The attempt number is sequential per campaign
-- (count of prior attempts + 1) and the name is human-readable:
--   "{CampaignName}_Attempt{N}_{yyyy-MM-dd_HH:mm}"   (time in IST)
-- The row is inserted with Status = 'Running'; the caller (Azure Function) finalizes it via
-- usp_CompleteIABatchCallLog once all calls are placed.
CREATE OR ALTER PROCEDURE [dbo].[usp_StartCampaignBatchCallAttempt]
    @CampaignId        INT,
    @CampaignName      NVARCHAR(200),
    @MagName           NVARCHAR(200),
    @InitiatedBy       VARCHAR(100),
    @TotalInterviewers INT,
    @NewId             INT OUTPUT,
    @AttemptNumber     INT OUTPUT,
    @AttemptName       NVARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRAN;

        -- Sequential attempt number per campaign. UPDLOCK/HOLDLOCK serializes concurrent opens
        -- so two attempts can't grab the same number.
        SELECT @AttemptNumber = COUNT(*) + 1
        FROM IABatchCallLog WITH (UPDLOCK, HOLDLOCK)
        WHERE CampaignId = @CampaignId;

        DECLARE @istNow DATETIME = DATEADD(MINUTE, 330, GETUTCDATE());
        SET @AttemptName = LTRIM(RTRIM(@CampaignName))
                         + N'_Attempt' + CAST(@AttemptNumber AS NVARCHAR(10))
                         + N'_' + FORMAT(@istNow, 'yyyy-MM-dd_HH:mm');

        INSERT INTO IABatchCallLog
            (MagName, InitiatedBy, InterviewersAttempted, CampaignId, AttemptNumber, AttemptName, Status)
        VALUES
            (@MagName, @InitiatedBy, @TotalInterviewers, @CampaignId, @AttemptNumber, @AttemptName, 'Running');

        SET @NewId = SCOPE_IDENTITY();

    COMMIT;
END
GO
