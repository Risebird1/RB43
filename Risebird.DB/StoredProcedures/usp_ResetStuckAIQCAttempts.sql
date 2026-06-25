IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ResetStuckAIQCAttempts'))
BEGIN
    DROP PROCEDURE dbo.usp_ResetStuckAIQCAttempts
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Resets interviews that were picked by a prior batch run but never received a saved QC response.
CREATE PROCEDURE [dbo].[usp_ResetStuckAIQCAttempts]
AS
BEGIN
    UPDATE InterviewComments
    SET IsAIQCAtempted = 0
    WHERE IsAIQCAtempted = 1
      AND OpenAIQCResponse IS NULL
END
GO
