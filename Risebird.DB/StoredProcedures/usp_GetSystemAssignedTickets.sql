-- usp_GetSystemAssignedTickets
-- Returns SupportTickets that were assigned ONLY to "System" and never to any human HR.
-- These are pure system-processed tickets with no manual intervention.
-- Also returns IRF info from CandidateResume (the first resume ingested for that ticket).
CREATE OR ALTER PROCEDURE [dbo].[usp_GetSystemAssignedTickets]
    @Page     INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    ;WITH SystemTicketIds AS (
        -- Tickets that were ever assigned to System
        SELECT DISTINCT TicketId
        FROM   dbo.SupportTicketHistory
        WHERE  ToStatus  = 'System'
          AND  EventType = 'AssignmentChange'
        UNION
        SELECT id AS TicketId
        FROM   dbo.SupportTickets
        WHERE  AssignedTo = 'System'
    ),
    HybridTicketIds AS (
        -- Tickets that were also assigned to a human (non-System, non-NULL)
        SELECT DISTINCT TicketId
        FROM   dbo.SupportTicketHistory
        WHERE  EventType = 'AssignmentChange'
          AND  ToStatus IS NOT NULL
          AND  ToStatus <> 'System'
          AND  ToStatus <> ''
        UNION
        SELECT id AS TicketId
        FROM   dbo.SupportTickets
        WHERE  AssignedTo IS NOT NULL
          AND  AssignedTo <> 'System'
          AND  AssignedTo <> ''
    ),
    PureSystemTicketIds AS (
        -- System tickets that were NEVER assigned to a human
        SELECT TicketId
        FROM   SystemTicketIds
        WHERE  TicketId NOT IN (SELECT TicketId FROM HybridTicketIds)
    ),
    TicketIRF AS (
        SELECT 
            cr.SupportTicketId,
            cr.IRFId,
            irf.IRFId AS IRFName,
            ISNULL(irf.JDTiltle, '') AS JDTitle,
            ROW_NUMBER() OVER (PARTITION BY cr.SupportTicketId ORDER BY cr.id ASC) AS rn
        FROM dbo.CandidateResume cr
        INNER JOIN dbo.IRF irf ON irf.id = cr.IRFId
        WHERE cr.SupportTicketId IS NOT NULL
    )
    SELECT
        t.Id,
        t.SenderEmail,
        t.Subject,
        t.MailContent,
        t.ReceivedAt,
        t.Intent,
        t.PrimarySpecialization,
        t.MailBodyIntent,
        t.AttachmentJobDescription,
        t.AttachmentPdf,
        t.AttachmentResumes,
        t.DriveFolderLink,
        t.SummaryMail,
        t.SummaryCandidates,
        t.SummaryInterviewScores,
        t.UserId,
        t.ClientId,
        ISNULL(c.Name, '') AS ClientName,
        t.Status,
        t.CreatedAt,
        t.UpdatedAt,
        t.AssignedTo,
        t.JobTitle,
        t.HasAutoScheduleCandidates,
        tirf.IRFId     AS IngestedIRFId,
        tirf.IRFName   AS IngestedIRFName,
        tirf.JDTitle   AS IngestedIRFJDTitle,
        COUNT(*) OVER() AS TotalCount
    FROM   dbo.SupportTickets t
    INNER JOIN PureSystemTicketIds s ON s.TicketId = t.Id
    LEFT JOIN dbo.Client c ON c.id = t.ClientId
    LEFT JOIN TicketIRF tirf ON tirf.SupportTicketId = t.Id AND tirf.rn = 1
    ORDER BY t.CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
