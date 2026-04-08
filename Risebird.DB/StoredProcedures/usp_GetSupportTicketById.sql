-- usp_GetSupportTicketById
-- Returns a single support ticket by ID with all relevant fields.
-- Used by the pending resumes modal to get ticket context.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetSupportTicketById]
    @TicketId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        t.Id,
        t.SenderEmail,
        t.Subject,
        t.Intent,
        t.PrimarySpecialization,
        t.AttachmentJobDescription,
        t.AttachmentPdf,
        t.AttachmentResumes,
        t.DriveFolderLink,
        t.SummaryMail,
        t.Status,
        t.AssignedTo,
        t.JobTitle,
        t.HasPendingResumes,
        t.ReceivedAt,
        t.CreatedAt,
        t.ClientId,
        t.UserId,
        c.Name AS ClientName
    FROM dbo.SupportTickets t
    LEFT JOIN dbo.Client c ON c.id = t.ClientId
    WHERE t.Id = @TicketId;
END
GO
