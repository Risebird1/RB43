CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateTrialFlagReview]
    @TrialFlagId     INT,
    @ReviewStatus    VARCHAR(20),
    @ReviewedBy      VARCHAR(150),
    @ReviewerComment NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS Ok;
        RETURN;
    END

    UPDATE dbo.TrialFlags
    SET ReviewStatus = @ReviewStatus,
        ReviewedAtUtc = GETUTCDATE(),
        ReviewedBy = @ReviewedBy,
        ReviewerComment = @ReviewerComment
    WHERE Id = @TrialFlagId
      AND ReviewStatus = 'pending';

    SELECT CAST(CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END AS BIT) AS Ok;
END
GO
