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

    -- EA interviewers (priority)
    ;WITH EAList AS (
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

    -- Profile interviewers not in EA
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
          AND ISNULL(ip.Isavailable, 0) <> 4
          AND LEN(ISNULL(ul.MobileNumber, '')) >= 10
          AND NOT EXISTS (SELECT 1 FROM EAProfileIds ep WHERE ep.InterviewerProfileId = ip.Id)
    )

    SELECT Id, InterviewerProfileId, InterviewerName, SenderEmail, Phone, ExperienceYears, PlivoCallStatus, Source, SortPriority
    FROM EAList
    UNION ALL
    SELECT Id, InterviewerProfileId, InterviewerName, SenderEmail, Phone, ExperienceYears, PlivoCallStatus, Source, SortPriority
    FROM ProfileList
    ORDER BY SortPriority, InterviewerName
END
GO
