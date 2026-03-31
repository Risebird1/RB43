-- usp_GetSystemAssignedTickets
-- Returns all SupportTickets that were ever assigned to "System" at any point,
-- regardless of current assignment. Uses SupportTicketHistory to find tickets
-- where ToStatus = 'System' with EventType = 'AssignmentChange'.
-- Also returns IRF info from CandidateResume (the first resume ingested for that ticket).
CREATE OR ALTER PROCEDURE [dbo].[usp_GetSystemAssignedTickets]
    @Page     INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    ;WITH SystemTicketIds AS (
        -- Tickets that were ever assigned to System via history
        SELECT DISTINCT TicketId
        FROM   dbo.SupportTicketHistory
        WHERE  ToStatus  = 'System'
          AND  EventType = 'AssignmentChange'
        UNION
        -- Tickets currently assigned to System (covers future direct assignments)
        SELECT id AS TicketId
        FROM   dbo.SupportTickets
        WHERE  AssignedTo = 'System'
    ),
    TicketIRF AS (
        SELECT 
            cr.SupportTicketId,
            cr.IRFId,
            irf.IRFId AS IRFName,
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
        tirf.IRFId     AS IngestedIRFId,
        tirf.IRFName   AS IngestedIRFName,
        COUNT(*) OVER() AS TotalCount
    FROM   dbo.SupportTickets t
    INNER JOIN SystemTicketIds s ON s.TicketId = t.Id
    LEFT JOIN dbo.Client c ON c.id = t.ClientId
    LEFT JOIN TicketIRF tirf ON tirf.SupportTicketId = t.Id AND tirf.rn = 1
    ORDER BY t.CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
