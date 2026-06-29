-- Corrective migration: realign ticket status for prospects who ARE onboarded.
--
-- Background: usp_AdvanceNaukriFunnelStage previously set TicketStatus UNCONDITIONALLY, even when the
-- requested funnel stage was behind the current one (a no-op for the stage). So a later/duplicate
-- action that requested 'AcknowledgementSent' + 'InProgress' (e.g. a manual re-onboard or ack resend)
-- left the FunnelStage correctly at 'Onboarded' but knocked the ticket status back to 'InProgress'
-- (or 'Open'). That produced records showing onboarded in the funnel yet "In Progress" as status.
--
-- The SP is now rank-guarded for the status too; this migration repairs the rows already affected.
-- Canonical onboarded ticket status is 'Closed' (set by both the self-registration and the
-- acknowledgement-acceptance completion paths).

BEGIN TRAN;

    DECLARE @before INT, @after INT;

    SELECT @before = COUNT(*)
    FROM NaukriProspectiveInterviewer
    WHERE FunnelStage = 'Onboarded'
      AND ISNULL(TicketStatus, '') NOT IN ('Closed', 'Onboarded');

    UPDATE NaukriProspectiveInterviewer
    SET TicketStatus = 'Closed',
        UpdatedAt    = GETUTCDATE()
    WHERE FunnelStage = 'Onboarded'
      AND ISNULL(TicketStatus, '') NOT IN ('Closed', 'Onboarded');

    SET @after = @@ROWCOUNT;

    -- Self-verification diagnostics.
    SELECT @before AS RecordsNeedingFix, @after AS RecordsUpdated;

    SELECT TicketStatus, COUNT(*) AS Cnt
    FROM NaukriProspectiveInterviewer
    WHERE FunnelStage = 'Onboarded'
    GROUP BY TicketStatus;

COMMIT;
GO
