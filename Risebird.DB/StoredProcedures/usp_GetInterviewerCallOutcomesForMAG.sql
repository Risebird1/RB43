CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerCallOutcomesForMAG]
    @MagName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AgId INT;
    SELECT TOP 1 @AgId = mag.AssessmentGroupId
    FROM MasterAssessmentGroup mag
    WHERE mag.MAGName = @MagName AND ISNULL(mag.IsDeleted, 0) = 0;

    -- EA interviewers (priority — listed first)
    ;WITH EAList AS (
        SELECT ea.Id, ea.InterviewerProfileId,
               ea.InterviewerName, ea.SenderEmail,
               COALESCE(ul.MobileNumber, ea.Phone) AS Phone,
               ip.Experience, ip.CurrentRole,
               ea.PlivoCallStatus, ea.PlivoIntent, ea.PlivoAssignmentHelp,
               ea.PlivoEmailReceived, ea.PlivoCallbackRequested,
               ea.PlivoCallDuration, ea.PlivoCalledBy, ea.PlivoCallSummary, ea.PlivoNotes,
               ea.PlivoCallAt, ea.Status,
               'EA' AS Source,
               1 AS SortPriority
        FROM InterviewerEmailAvailability ea
        LEFT JOIN UserList ul ON ea.UserId = ul.Id
        LEFT JOIN InterviewerProfile ip ON ea.InterviewerProfileId = ip.Id
        WHERE ea.MagName = @MagName
          AND ea.Status IN ('Open', 'InProgress', 'Verified')
    ),

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
        SELECT ip.Id, ip.Id AS InterviewerProfileId,
               ip.NickName AS InterviewerName,
               ul.EmailId AS SenderEmail,
               ul.MobileNumber AS Phone,
               ip.Experience, ip.CurrentRole,
               CAST(NULL AS VARCHAR(50)) AS PlivoCallStatus,
               CAST(NULL AS VARCHAR(50)) AS PlivoIntent,
               CAST(NULL AS BIT) AS PlivoAssignmentHelp,
               CAST(NULL AS BIT) AS PlivoEmailReceived,
               CAST(NULL AS BIT) AS PlivoCallbackRequested,
               CAST(NULL AS INT) AS PlivoCallDuration,
               CAST(NULL AS VARCHAR(150)) AS PlivoCalledBy,
               CAST(NULL AS NVARCHAR(MAX)) AS PlivoCallSummary,
               CAST(NULL AS NVARCHAR(MAX)) AS PlivoNotes,
               CAST(NULL AS DATETIME) AS PlivoCallAt,
               'N/A' AS Status,
               'Profile' AS Source,
               2 AS SortPriority
        FROM AssessmentGroupInterviewers agi
        INNER JOIN InterviewerProfile ip ON ip.Id = agi.InterviewerProfileId
        INNER JOIN UserList ul ON ul.Id = ip.UserId
        WHERE agi.AssessmentGroupId = @AgId
          AND ISNULL(ip.IsDeleted, 0) = 0
          AND ISNULL(ip.Isavailable, 0) <> 4
          AND NOT EXISTS (SELECT 1 FROM EAProfileIds ep WHERE ep.InterviewerProfileId = ip.Id)
    )

    SELECT Id, InterviewerProfileId, InterviewerName, SenderEmail, Phone,
           Experience, CurrentRole,
           PlivoCallStatus, PlivoIntent, PlivoAssignmentHelp,
           PlivoEmailReceived, PlivoCallbackRequested,
           PlivoCallDuration, PlivoCalledBy, PlivoCallSummary, PlivoNotes,
           PlivoCallAt, Status, Source, SortPriority
    FROM EAList
    UNION ALL
    SELECT Id, InterviewerProfileId, InterviewerName, SenderEmail, Phone,
           Experience, CurrentRole,
           PlivoCallStatus, PlivoIntent, PlivoAssignmentHelp,
           PlivoEmailReceived, PlivoCallbackRequested,
           PlivoCallDuration, PlivoCalledBy, PlivoCallSummary, PlivoNotes,
           PlivoCallAt, Status, Source, SortPriority
    FROM ProfileList
    ORDER BY SortPriority, PlivoCallAt DESC, Id DESC
END
GO
