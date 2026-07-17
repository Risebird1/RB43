-- ============================================================================================
-- usp_UpdateNaukriLinkedInVerdict
--
-- Overwrites the full LinkedIn background-verification verdict (URL + match + confidence + reason)
-- for a prospect. Used by the operator-triggered "AI Verify LinkedIn" second-opinion action in the
-- NaukriOnboarding Manage modal, which sends the prospect's details to an OpenAI assistant and stores
-- the returned verdict here. The prior verdict (e.g. the one captured from the Relay agent at
-- ingestion) is replaced; the change is recorded on the ticket history for audit.
-- ============================================================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateNaukriLinkedInVerdict]
    @Id                 INT,
    @LinkedInUrl        NVARCHAR(2000)  = NULL,
    @LinkedInMatch      VARCHAR(10)     = NULL,
    @LinkedInConfidence INT             = NULL,
    @LinkedInReason     NVARCHAR(2000)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE NaukriProspectiveInterviewer
    SET LinkedInUrl        = @LinkedInUrl,
        LinkedInMatch      = @LinkedInMatch,
        LinkedInConfidence = @LinkedInConfidence,
        LinkedInReason     = @LinkedInReason,
        UpdatedAt          = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
