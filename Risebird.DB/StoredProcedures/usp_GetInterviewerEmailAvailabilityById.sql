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
        ea.PlivoCallStatus, ea.PlivoConversationId, ea.PlivoCallAt, ea.PlivoCallDuration,
        ea.PlivoCallTranscript, ea.PlivoCallSummary, ea.PlivoIntent,
        ea.PlivoAssignmentHelp, ea.PlivoEmailReceived, ea.PlivoCallbackRequested,
        ea.PlivoNotes, ea.PlivoCalledBy,
        ip.NickName           AS InterviewerNickName,
        ip.CurrentRole,
        ip.Experience,
        ip.isVerified         AS IsProfileVerified,
        ip.IsEmployed,
        ip.CompanyName,
        ip.ResumeLink         AS ProfileResumeLink,
        ip.AssessmentGroupId  AS ExistingAssessmentGroupIds,
        ul.EmailId            AS UserEmail,
        ul.MobileNumber       AS UserMobileNumber,
        magInfo.MAGName       AS MasterAssessmentGroupName,
        magInfo.AssessmentGroupId AS ResolvedAssessmentGroupId,
        magInfo.AssessmentGroupName,
        1                     AS TotalCount
    FROM InterviewerEmailAvailability ea
    LEFT JOIN InterviewerProfile ip ON ip.Id = ea.InterviewerProfileId
    LEFT JOIN UserList ul ON ul.id = ea.UserId
    OUTER APPLY (
        SELECT TOP 1 mag.MAGName, mag.AssessmentGroupId, ag.AssessmentGroup AS AssessmentGroupName
        FROM MasterAssessmentGroup mag
        LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
        WHERE LTRIM(RTRIM(LOWER(mag.MAGName))) = LTRIM(RTRIM(LOWER(ea.MagName)))
          AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
    ) magInfo
    WHERE ea.Id = @Id
END
GO
