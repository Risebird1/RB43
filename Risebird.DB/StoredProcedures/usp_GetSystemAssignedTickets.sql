-- usp_GetSystemAssignedTickets
-- Returns SupportTickets that were assigned ONLY to "System" and never to any human HR.
-- These are pure system-processed tickets with no manual intervention.
-- Also returns IRF info from CandidateResume (the first resume ingested for that ticket).
-- Optimized for performance with large datasets.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetSystemAssignedTickets]
    @Page     INT = 1,
    @PageSize INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page     < 1   SET @Page     = 1;
    IF @PageSize < 1   SET @PageSize = 25;
    IF @PageSize > 200 SET @PageSize = 200;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    -- Pre-compute ticket categories
    ;WITH SystemTicketIds AS (
        SELECT DISTINCT TicketId
        FROM dbo.SupportTicketHistory
        WHERE ToStatus = 'System' AND EventType = 'AssignmentChange'
        UNION
        SELECT id FROM dbo.SupportTickets WHERE AssignedTo = 'System'
    ),
    HumanTicketIds AS (
        SELECT DISTINCT TicketId
        FROM dbo.SupportTicketHistory
        WHERE EventType = 'AssignmentChange' AND ToStatus IS NOT NULL AND ToStatus <> 'System' AND ToStatus <> ''
        UNION
        SELECT id FROM dbo.SupportTickets WHERE AssignedTo IS NOT NULL AND AssignedTo <> 'System' AND AssignedTo <> ''
    ),
    PureSystemIds AS (
        SELECT TicketId FROM SystemTicketIds WHERE TicketId NOT IN (SELECT TicketId FROM HumanTicketIds)
    ),
    TicketIRF AS (
        SELECT cr.SupportTicketId, cr.IRFId, irf.IRFId AS IRFName, ISNULL(irf.JDTiltle, '') AS JDTitle,
               ROW_NUMBER() OVER (PARTITION BY cr.SupportTicketId ORDER BY cr.id ASC) AS rn
        FROM dbo.CandidateResume cr
        INNER JOIN dbo.IRF irf ON irf.id = cr.IRFId
        INNER JOIN PureSystemIds ps ON ps.TicketId = cr.SupportTicketId
    ),
    ResultSet AS (
        SELECT
            t.Id, t.SenderEmail, t.Subject, t.MailContent, t.ReceivedAt, t.Intent,
            t.PrimarySpecialization, t.MailBodyIntent, t.AttachmentJobDescription,
            t.AttachmentPdf, t.AttachmentResumes, t.DriveFolderLink, t.SummaryMail,
            t.SummaryCandidates, t.SummaryInterviewScores, t.UserId, t.ClientId,
            ISNULL(c.Name, '') AS ClientName, t.Status, t.CreatedAt, t.UpdatedAt,
            t.AssignedTo, t.JobTitle, t.HasAutoScheduleCandidates, t.HasPendingResumes,
            tirf.IRFId AS IngestedIRFId, tirf.IRFName AS IngestedIRFName, tirf.JDTitle AS IngestedIRFJDTitle
        FROM dbo.SupportTickets t
        INNER JOIN PureSystemIds s ON s.TicketId = t.Id
        LEFT JOIN dbo.Client c ON c.id = t.ClientId
        LEFT JOIN TicketIRF tirf ON tirf.SupportTicketId = t.Id AND tirf.rn = 1
    )
    SELECT *, (SELECT COUNT(*) FROM ResultSet) AS TotalCount
    FROM ResultSet
    ORDER BY CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
