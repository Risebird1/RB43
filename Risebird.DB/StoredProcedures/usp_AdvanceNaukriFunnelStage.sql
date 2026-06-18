-- Advances the funnel FORWARD-ONLY (never regresses) and optionally sets the operational
-- ticket status. Rank comparison uses dbo.fn_NaukriFunnelStageRank so the ordinal order of
-- the happy-path stages lives in exactly one place. Terminal exit stages (rank 0) are NOT set
-- through here — use usp_SetNaukriFunnelOutcome for those.
CREATE OR ALTER PROCEDURE [dbo].[usp_AdvanceNaukriFunnelStage]
    @Id           INT,
    @FunnelStage  VARCHAR(50),
    @TicketStatus VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET FunnelStage = CASE
            WHEN dbo.fn_NaukriFunnelStageRank(@FunnelStage) >= dbo.fn_NaukriFunnelStageRank(FunnelStage)
            THEN @FunnelStage ELSE FunnelStage END,
        TicketStatus = ISNULL(@TicketStatus, TicketStatus),
        UpdatedAt    = GETUTCDATE()
    WHERE Id = @Id;
END
GO
