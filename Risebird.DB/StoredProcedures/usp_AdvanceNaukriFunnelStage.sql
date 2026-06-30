-- Advances the funnel FORWARD-ONLY (never regresses) and optionally sets the operational
-- ticket status. Rank comparison uses dbo.fn_NaukriFunnelStageRank so the ordinal order of
-- the happy-path stages lives in exactly one place. Terminal exit stages (rank 0) are NOT set
-- through here — use usp_SetNaukriFunnelOutcome for those.
--
-- The ticket status is only applied when the funnel is actually advancing (or holding at the
-- same stage). If the requested stage is BEHIND the current one, this call is a no-op for the
-- funnel, so the status must NOT be downgraded either — otherwise a later/duplicate action
-- (e.g. a manual re-onboard or ack resend that requests 'AcknowledgementSent'/'InProgress')
-- would knock an already-'Onboarded' prospect's ticket back to 'InProgress'.
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
        TicketStatus = CASE
            WHEN @TicketStatus IS NULL THEN TicketStatus
            WHEN dbo.fn_NaukriFunnelStageRank(@FunnelStage) >= dbo.fn_NaukriFunnelStageRank(FunnelStage)
            THEN @TicketStatus ELSE TicketStatus END,
        UpdatedAt    = GETUTCDATE()
    WHERE Id = @Id;
END
GO
