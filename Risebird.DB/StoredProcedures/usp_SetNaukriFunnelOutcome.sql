-- Sets a TERMINAL/branch funnel outcome (Duplicate, NotInterested, Unsubscribe, Cold)
-- UNCONDITIONALLY — these are exits off the happy path, so the forward-only rank guard
-- used by usp_AdvanceNaukriFunnelStage does not apply. Optionally sets the ticket status.
CREATE OR ALTER PROCEDURE [dbo].[usp_SetNaukriFunnelOutcome]
    @Id           INT,
    @FunnelStage  VARCHAR(50),
    @TicketStatus VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET FunnelStage  = @FunnelStage,
        TicketStatus = ISNULL(@TicketStatus, TicketStatus),
        UpdatedAt    = GETUTCDATE()
    WHERE Id = @Id;
END
GO
