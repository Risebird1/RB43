IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetInterviewerEmailAvailabilityById')
    DROP PROCEDURE usp_GetInterviewerEmailAvailabilityById
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerEmailAvailabilityById]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ea.Id, ea.SenderEmail, ea.UserId, ea.InterviewerProfileId,
        ea.Subject, ea.MailContent, ea.ReceivedAt,
        ea.JobTitle, ea.InterviewerName, ea.Phone, ea.MagName, ea.IRFReference,
        ea.HasLoginCredentials, ea.SlotsPerDay, ea.TimeFrame,
        ea.AvailabilityIntent, ea.MailBodyIntent, ea.ConcernsOrNotInterested,
        ea.ResumeFitment, ea.ResumeLink, ea.HasAttachment,
        ea.Status,
        ea.EmploymentVerified, ea.ResumeAIAnalysisCompleted, ea.ResumeAIAnalysisResult,
        ea.AssessmentGroupId, ea.AssessmentGroupAssignedAt,
        ea.ProcessedBy, ea.CreatedAt, ea.UpdatedAt,
        ip.NickName           AS InterviewerNickName,
        ip.CurrentRole,
        ip.Experience,
        ip.isVerified         AS IsProfileVerified,
        ip.IsEmployed,
        ip.CompanyName,
        ip.ResumeLink         AS ProfileResumeLink,
        ip.AssessmentGroupId  AS ExistingAssessmentGroupIds,
        ul.EmailId            AS UserEmail,
        mag.MAGName           AS MasterAssessmentGroupName,
        mag.AssessmentGroupId AS ResolvedAssessmentGroupId,
        ag.AssessmentGroup    AS AssessmentGroupName,
        1                     AS TotalCount
    FROM InterviewerEmailAvailability ea
    LEFT JOIN InterviewerProfile ip ON ip.Id = ea.InterviewerProfileId
    LEFT JOIN UserList ul ON ul.id = ea.UserId
    LEFT JOIN MasterAssessmentGroup mag
        ON LTRIM(RTRIM(LOWER(mag.MAGName))) = LTRIM(RTRIM(LOWER(ea.MagName)))
        AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
    LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
    WHERE ea.Id = @Id
END
GO
