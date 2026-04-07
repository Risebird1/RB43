-- usp_GetPendingResumesForTicket
-- Returns all pending resume records for a support ticket with full context.
-- Used by the HR review modal to provide comprehensive data for decision-making.
-- Includes IRF change tracking for accuracy analysis.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetPendingResumesForTicket]
    @TicketId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        -- Pending Resume fields
        pr.Id,
        pr.SupportTicketId,
        pr.ResumeUrl,
        pr.ResumeFileName,
        pr.CandidateName,
        pr.CandidateEmail,
        pr.ResolvedIRFId,
        pr.ResolvedIRFStringId,
        pr.ResolvedJobTitle,
        pr.ResolvedHREmail,
        pr.HasSchedulingData,
        pr.ScheduledInterviewDateTime,
        pr.ScheduleTimezone,
        pr.Status,
        pr.IngestedResumeId,
        pr.ProcessedAt,
        pr.ProcessedBy,
        pr.CreatedAt,
        
        -- Original system-resolved IRF (for tracking)
        pr.OriginalResolvedIRFId,
        pr.OriginalResolvedIRFStringId,
        pr.IRFChangedByHR,
        pr.IRFChangedAt,
        pr.IRFChangeReason,
        
        -- Current IRF info if resolved
        irf.JDTiltle                AS IRFJobTitle,
        irf.SkillCluster            AS IRFSkillCluster,
        irf.StartDate               AS IRFStartDate,
        irf.EndDate                 AS IRFEndDate,
        irf.NoOfPosition            AS IRFPositions,
        irf.JDLink                  AS IRFJDLink,
        
        -- Original IRF info (for comparison)
        origIrf.JDTiltle            AS OriginalIRFJobTitle,
        
        -- Client info
        cl.Name                     AS ClientName,
        cl.tenantId                 AS TenantId,
        cl.id                       AS ClientId,
        
        -- Ticket context (email details)
        st.SenderEmail              AS TicketSenderEmail,
        st.Subject                  AS TicketSubject,
        st.MailContent              AS TicketMailContent,
        st.Intent                   AS TicketIntent,
        st.PrimarySpecialization    AS TicketPrimarySpecialization,
        st.MailBodyIntent           AS TicketMailBodyIntent,
        st.JobTitle                 AS TicketJobTitle,
        st.ReceivedAt               AS TicketReceivedAt,
        st.DriveFolderLink          AS TicketDriveFolderLink,
        st.SummaryMail              AS TicketSummaryMail,
        st.SummaryCandidates        AS TicketSummaryCandidates,
        st.AttachmentJobDescription AS TicketHasJD,
        st.AttachmentPdf            AS TicketHasPdf,
        st.AttachmentResumes        AS TicketHasResumes,
        
        -- HR info from resolved IRF
        LTRIM(RTRIM(ISNULL(hrUser.FirstName, '') + ' ' + ISNULL(hrUser.LastName, ''))) AS ResolvedHRName
        
    FROM   dbo.SupportTicketPendingResumes pr
    INNER JOIN dbo.SupportTickets st ON st.Id = pr.SupportTicketId
    LEFT JOIN dbo.IRF irf ON irf.id = pr.ResolvedIRFId
    LEFT JOIN dbo.IRF origIrf ON origIrf.id = pr.OriginalResolvedIRFId
    LEFT JOIN dbo.Client cl ON cl.id = COALESCE(irf.ClientId, st.ClientId)
    LEFT JOIN dbo.UserList hrUser ON hrUser.EmailId = pr.ResolvedHREmail
    WHERE  pr.SupportTicketId = @TicketId
    ORDER BY pr.Id ASC;
END
GO
