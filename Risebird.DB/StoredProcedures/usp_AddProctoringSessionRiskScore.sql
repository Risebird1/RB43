IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddProctoringSessionRiskScore'))
BEGIN
    DROP PROCEDURE dbo.usp_AddProctoringSessionRiskScore
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Applies a risk-score delta (sum of RiskScorer.ScoreEvent over the newly inserted events in one
-- batch) and returns the new cumulative, clamped [0,100] score in a single round trip.
CREATE PROCEDURE [dbo].[usp_AddProctoringSessionRiskScore]
    @SessionToken VARCHAR(64),
    @Delta INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ProctoringSessions
    SET [RiskScore] = CASE
            WHEN [RiskScore] + @Delta < 0 THEN 0
            WHEN [RiskScore] + @Delta > 100 THEN 100
            ELSE [RiskScore] + @Delta
        END
    WHERE [SessionToken] = @SessionToken;

    SELECT [RiskScore] FROM dbo.ProctoringSessions WHERE [SessionToken] = @SessionToken
END
GO
