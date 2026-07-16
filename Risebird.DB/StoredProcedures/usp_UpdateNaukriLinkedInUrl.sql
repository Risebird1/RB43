-- ============================================================================================
-- usp_UpdateNaukriLinkedInUrl
--
-- Manually overrides the LinkedIn profile URL for a prospect. This is an operator-only action
-- exposed through the NaukriOnboarding Manage modal — the automated pipeline never calls it.
-- Only the URL is changed; the match/confidence/reason verdict captured at ingestion is left
-- intact so the audit trail of the original AI decision is preserved.
-- ============================================================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateNaukriLinkedInUrl]
    @Id          INT,
    @LinkedInUrl NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE NaukriProspectiveInterviewer
    SET LinkedInUrl = @LinkedInUrl,
        UpdatedAt   = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
