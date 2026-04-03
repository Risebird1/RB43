-- usp_GetTicketCounts
-- Returns counts for all three tabs in a single call.
-- Much faster than calling full ticket queries just to get counts.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetTicketCounts]
AS
BEGIN
    SET NOCOUNT ON;

    -- Pre-compute ticket categories once
    DECLARE @SystemTickets TABLE (TicketId INT PRIMARY KEY);
    DECLARE @HumanTickets TABLE (TicketId INT PRIMARY KEY);

    INSERT INTO @SystemTickets (TicketId)
    SELECT DISTINCT TicketId
    FROM dbo.SupportTicketHistory
    WHERE ToStatus = 'System' AND EventType = 'AssignmentChange'
    UNION
    SELECT id FROM dbo.SupportTickets WHERE AssignedTo = 'System';

    INSERT INTO @HumanTickets (TicketId)
    SELECT DISTINCT TicketId
    FROM dbo.SupportTicketHistory
    WHERE EventType = 'AssignmentChange' AND ToStatus IS NOT NULL AND ToStatus <> 'System' AND ToStatus <> ''
    UNION
    SELECT id FROM dbo.SupportTickets WHERE AssignedTo IS NOT NULL AND AssignedTo <> 'System' AND AssignedTo <> '';

    -- System Only: tickets in System set but NOT in Human set
    DECLARE @SystemOnlyCount INT = (
        SELECT COUNT(*) FROM @SystemTickets s WHERE NOT EXISTS (SELECT 1 FROM @HumanTickets h WHERE h.TicketId = s.TicketId)
    );

    -- Hybrid: tickets in BOTH sets
    DECLARE @HybridCount INT = (
        SELECT COUNT(*) FROM @SystemTickets s WHERE EXISTS (SELECT 1 FROM @HumanTickets h WHERE h.TicketId = s.TicketId)
    );

    -- Manual Review: tickets NOT in System set
    DECLARE @ManualCount INT = (
        SELECT COUNT(*) FROM dbo.SupportTickets t WHERE NOT EXISTS (SELECT 1 FROM @SystemTickets s WHERE s.TicketId = t.Id)
            AND (t.AssignedTo IS NULL OR t.AssignedTo <> 'System')
    );

    SELECT 
        @ManualCount AS ManualReviewCount,
        @SystemOnlyCount AS SystemOnlyCount,
        @HybridCount AS HybridCount;
END
GO
