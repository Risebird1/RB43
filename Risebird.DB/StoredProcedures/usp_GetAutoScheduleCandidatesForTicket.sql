-- usp_GetAutoScheduleCandidatesForTicket
-- Returns candidate resume records with auto-schedule data for a specific support ticket.
-- Shows both extracted data (from email) and parsed data (from resume parsing).
-- Used by the HR review modal to verify and schedule interviews.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetAutoScheduleCandidatesForTicket]
    @TicketId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cr.id                            AS CandidateResumeId,
        cr.IRFId,
        cr.ResumeLink,
        cr.AddedOn,
        cr.IsProcessed,
        cr.IsResumeParsed,
        cr.IsResumeParsingFailed,
        cr.IsEmailMissing,
        cr.ReasonBehindNotProcessed,
        cr.IsAutoScheduled,
        cr.ScheduledInterviewDateTime,
        cr.ExtractedCandidateName,
        cr.ExtractedCandidateEmail,
        irf.IRFId                        AS SkillChannelId,
        irf.JDTiltle                     AS JobTitle,
        irf.ClientId,
        cl.Name                          AS ClientName,
        cl.tenantId                      AS TenantId,
        -- Parsed candidate data (from Candidate table if exists)
        c.id                             AS CandidateId,
        c.Name                           AS ParsedCandidateName,
        c.EmailId                        AS ParsedCandidateEmail,
        c.Phone                          AS ParsedCandidatePhone,
        -- Scheduling status
        CASE 
            WHEN cr.IsResumeParsed = 1 AND ISNULL(cr.IsResumeParsingFailed, 0) = 0 THEN 'Parsed'
            WHEN cr.IsResumeParsingFailed = 1 THEN 'Parsing Failed'
            WHEN cr.IsResumeParsed = 0 OR cr.IsResumeParsed IS NULL THEN 'Pending Parse'
            ELSE 'Unknown'
        END                              AS ParsingStatus,
        -- Check if interview already scheduled
        CASE 
            WHEN EXISTS (
                SELECT 1 FROM dbo.Meeting m
                WHERE m.CandidateId = c.id
                  AND m.IRFId = cr.IRFId
                  AND m.IsDeleted = 0
                  AND m.Date >= CAST(GETDATE() AS DATE)
            ) THEN 1
            ELSE 0
        END                              AS IsAlreadyScheduled,
        -- Email match indicator
        CASE 
            WHEN cr.ExtractedCandidateEmail IS NOT NULL 
                 AND c.EmailId IS NOT NULL 
                 AND LOWER(LTRIM(RTRIM(cr.ExtractedCandidateEmail))) = LOWER(LTRIM(RTRIM(c.EmailId)))
            THEN 1
            WHEN cr.ExtractedCandidateEmail IS NULL OR c.EmailId IS NULL
            THEN NULL -- Can't compare
            ELSE 0
        END                              AS EmailMatches
    FROM   dbo.CandidateResume cr
    INNER JOIN dbo.IRF irf ON irf.id = cr.IRFId
    LEFT JOIN dbo.Client cl ON cl.id = irf.ClientId
    LEFT JOIN dbo.Candidate c ON c.CandidateResumeId = cr.id
    WHERE  cr.SupportTicketId = @TicketId
      AND  cr.IsAutoScheduled = 1
    ORDER BY cr.id ASC;
END
GO
