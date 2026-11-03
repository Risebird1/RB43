CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewersForMAGBatchCall]
    @MagName      NVARCHAR(200),
    @OnlyUncalled BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AgId INT;
    SELECT TOP 1 @AgId = mag.AssessmentGroupId
    FROM MasterAssessmentGroup mag
    WHERE mag.MAGName = @MagName AND ISNULL(mag.IsDeleted, 0) = 0;

    -- Interviewers who already signaled a favorable availability for this MAG within the
    -- last 48 hours. They are "recently available" and must be excluded from outbound
    -- outreach (we already know they're available — no need to call/email them again).
    ;WITH RecentlyAvailable AS (
        SELECT ea.InterviewerProfileId, ea.SenderEmail
        FROM InterviewerEmailAvailability ea
        WHERE ea.MagName = @MagName
          AND ea.ReceivedAt >= DATEADD(HOUR, -48, GETUTCDATE())
          AND dbo.fn_IsFavorableAvailabilityIntent(ea.AvailabilityIntent, ea.MailBodyIntent, ea.ConcernsOrNotInterested) = 1
    ),

    -- EA interviewers (priority) — exclude inactive/deleted/dropped
    EAList AS (
        SELECT ea.Id,
               ea.InterviewerProfileId,
               ea.InterviewerName,
               ea.SenderEmail,
               COALESCE(ul.MobileNumber, ea.Phone) AS Phone,
               ip.Experience AS ExperienceYears,
               ea.PlivoCallStatus,
               'EA' AS Source,
               1 AS SortPriority
        FROM InterviewerEmailAvailability ea
        LEFT JOIN UserList ul ON ea.UserId = ul.Id
        LEFT JOIN InterviewerProfile ip ON ea.InterviewerProfileId = ip.Id
        WHERE ea.MagName = @MagName
          AND ea.Status IN ('Open', 'InProgress', 'Verified')
          AND LEN(ISNULL(COALESCE(ul.MobileNumber, ea.Phone), '')) >= 10
          AND (@OnlyUncalled = 0 OR ISNULL(ea.PlivoCallStatus, '') = '')
          AND (ul.Id IS NULL OR ul.IsActive = 1)
          -- An EA row can be UNLINKED (ea.UserId IS NULL) even when the sender is actually a
          -- registered interviewer — this happens when the record was captured while the
          -- interviewer was already inactive (usp_InsertInterviewerEmailAvailability only links
          -- UserId for active users). The join-based IsActive check above cannot catch that case,
          -- so fall back to an email match to guarantee an inactive interviewer is never re-contacted.
          AND NOT EXISTS (
              SELECT 1 FROM UserList ulEmail
              WHERE ulEmail.EmailId = ea.SenderEmail
                AND ISNULL(ulEmail.IsActive, 1) = 0
          )
          AND (ip.Id IS NULL OR (ISNULL(ip.IsDeleted, 0) = 0 AND ISNULL(ip.Isavailable, 0) NOT IN (4, 5) AND ISNULL(ip.CallConsent, 1) <> 0))
          AND NOT EXISTS (
              SELECT 1 FROM RecentlyAvailable ra
              WHERE (ea.InterviewerProfileId IS NOT NULL AND ra.InterviewerProfileId = ea.InterviewerProfileId)
                 OR ra.SenderEmail = ea.SenderEmail
          )
    ),

    -- Distinct EA profile IDs for this MAG to exclude from Profile list
    EAProfileIds AS (
        SELECT ea.InterviewerProfileId
        FROM InterviewerEmailAvailability ea
        WHERE ea.MagName = @MagName
          AND ea.InterviewerProfileId IS NOT NULL
          AND ea.Status IN ('Open', 'InProgress', 'Verified')
        GROUP BY ea.InterviewerProfileId
    ),

    -- Profile interviewers not in EA — exclude inactive/deleted/dropped
    ProfileList AS (
        SELECT ip.Id,
               ip.Id AS InterviewerProfileId,
               ip.NickName AS InterviewerName,
               ul.EmailId AS SenderEmail,
               ul.MobileNumber AS Phone,
               ip.Experience AS ExperienceYears,
               CAST(NULL AS VARCHAR(50)) AS PlivoCallStatus,
               'Profile' AS Source,
               2 AS SortPriority
        FROM AssessmentGroupInterviewers agi
        INNER JOIN InterviewerProfile ip ON ip.Id = agi.InterviewerProfileId
        INNER JOIN UserList ul ON ul.Id = ip.UserId
        WHERE agi.AssessmentGroupId = @AgId
          AND ISNULL(ip.IsDeleted, 0) = 0
          AND ISNULL(ip.Isavailable, 0) NOT IN (4, 5) AND ISNULL(ip.CallConsent, 1) <> 0
          AND ul.IsActive = 1
          AND LEN(ISNULL(ul.MobileNumber, '')) >= 10
          AND NOT EXISTS (SELECT 1 FROM EAProfileIds ep WHERE ep.InterviewerProfileId = ip.Id)
          AND NOT EXISTS (
              SELECT 1 FROM RecentlyAvailable ra
              WHERE ra.InterviewerProfileId = ip.Id OR ra.SenderEmail = ul.EmailId
          )
    )

    SELECT Id, InterviewerProfileId, InterviewerName, SenderEmail, Phone, ExperienceYears, PlivoCallStatus, Source, SortPriority
    FROM EAList
    UNION ALL
    SELECT Id, InterviewerProfileId, InterviewerName, SenderEmail, Phone, ExperienceYears, PlivoCallStatus, Source, SortPriority
    FROM ProfileList
    ORDER BY SortPriority, InterviewerName
END
GO
