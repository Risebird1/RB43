/*
================================================================================
RETIRED — usp_GetTrialFlagsForReview

Replaced by the lazy two-tier read model (trial-read-architecture):
  • usp_GetTrialFlagsForReview_List  — paginated light grid
  • usp_GetTrialFlagDetail           — on-demand heavy row detail

This stub remains so accidental callers fail fast instead of timing out production.
Do not restore the wide single-shot query.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetTrialFlagsForReview]
    @ReviewStatus VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    RAISERROR(
        'usp_GetTrialFlagsForReview is retired. Use usp_GetTrialFlagsForReview_List + usp_GetTrialFlagDetail.',
        16,
        1);
END
GO
