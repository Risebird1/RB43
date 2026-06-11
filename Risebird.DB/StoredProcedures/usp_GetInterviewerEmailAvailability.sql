IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetInterviewerEmailAvailability')
    DROP PROCEDURE usp_GetInterviewerEmailAvailability
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerEmailAvailability]
    @Status         VARCHAR(50)  = NULL,
    @SenderEmail    VARCHAR(150) = NULL,
    @ResumeFitment  VARCHAR(50)  = NULL,
    @DateFrom       DATETIME     = NULL,
    @DateTo         DATETIME     = NULL,
    @Source              VARCHAR(50)  = NULL,
    @IsAutoClosed        BIT          = NULL,
    @IsInterviewerActive BIT          = NULL,
    @RecentlyAvailable   BIT          = NULL,
    @Page                INT          = 1,
    @PageSize            INT          = 25
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page < 1 SET @Page = 1;
    IF @PageSize < 1 SET @PageSize = 25;

    ;WITH BaseData AS (
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
            ea.Source, ea.IsAutoClosed, ea.AutoClosedBy, ea.AutoClosedAt,
            ip.NickName           AS InterviewerNickName,
            ip.CurrentRole,
            ip.Experience,
            ip.isVerified         AS IsProfileVerified,
            ip.IsEmployed,
            ip.CompanyName,
            ip.ResumeLink         AS ProfileResumeLink,
            ul.MobileNumber       AS UserMobileNumber,
            ISNULL(ul.IsActive, 1) AS IsInterviewerActive,
            ul.InactivationType,
            COUNT(*) OVER()       AS TotalCount,
            -- Operational status buckets: Open / InProgress / Closed always sum to the total.
            -- The legacy 'Verified' value (AI résumé-fit) is folded into InProgress; anything
            -- that is neither Open nor Closed (incl. NULL) counts as InProgress.
            SUM(CASE WHEN ISNULL(ea.Status, 'Open') = 'Open'                       THEN 1 ELSE 0 END) OVER() AS OpenCount,
            SUM(CASE WHEN ISNULL(ea.Status, 'Open') NOT IN ('Open', 'Closed')      THEN 1 ELSE 0 END) OVER() AS InProgressCount,
            SUM(CASE WHEN ISNULL(ea.Status, 'Open') = 'Closed'                     THEN 1 ELSE 0 END) OVER() AS ClosedCount,
            -- "Recently available": favorable availability signal received within the last 48 hours.
            CAST(CASE WHEN ea.ReceivedAt >= DATEADD(HOUR, -48, GETUTCDATE())
                       AND dbo.fn_IsFavorableAvailabilityIntent(ea.AvailabilityIntent, ea.MailBodyIntent, ea.ConcernsOrNotInterested) = 1
                      THEN 1 ELSE 0 END AS BIT) AS IsRecentlyAvailable,
            SUM(CASE WHEN ea.ReceivedAt >= DATEADD(HOUR, -48, GETUTCDATE())
                      AND dbo.fn_IsFavorableAvailabilityIntent(ea.AvailabilityIntent, ea.MailBodyIntent, ea.ConcernsOrNotInterested) = 1
                     THEN 1 ELSE 0 END) OVER() AS RecentlyAvailableCount,
            ROW_NUMBER() OVER (ORDER BY ea.ReceivedAt DESC, ea.Id DESC) AS RowNum
        FROM InterviewerEmailAvailability ea
        LEFT JOIN InterviewerProfile ip ON ip.Id = ea.InterviewerProfileId
        LEFT JOIN UserList ul ON ul.id = ea.UserId
        WHERE
            -- 'InProgress' filter also matches the legacy 'Verified' value (folded into InProgress).
            (@Status IS NULL
                OR ea.Status = @Status
                OR (@Status = 'InProgress' AND ea.Status = 'Verified')
                OR (@Status = 'Open' AND ea.Status IS NULL))
            AND (@SenderEmail IS NULL OR ea.SenderEmail LIKE '%' + @SenderEmail + '%')
            AND (@ResumeFitment IS NULL OR ea.ResumeFitment = @ResumeFitment)
            AND (@Source IS NULL OR ea.Source LIKE @Source + '%')
            AND (@IsAutoClosed IS NULL OR ISNULL(ea.IsAutoClosed, 0) = @IsAutoClosed)
            AND (@RecentlyAvailable IS NULL
                 OR (@RecentlyAvailable = 1
                     AND ea.ReceivedAt >= DATEADD(HOUR, -48, GETUTCDATE())
                     AND dbo.fn_IsFavorableAvailabilityIntent(ea.AvailabilityIntent, ea.MailBodyIntent, ea.ConcernsOrNotInterested) = 1)
                 OR (@RecentlyAvailable = 0
                     AND NOT (ea.ReceivedAt >= DATEADD(HOUR, -48, GETUTCDATE())
                              AND dbo.fn_IsFavorableAvailabilityIntent(ea.AvailabilityIntent, ea.MailBodyIntent, ea.ConcernsOrNotInterested) = 1)))
            AND (@DateFrom IS NULL OR ea.ReceivedAt >= @DateFrom)
            AND (@DateTo IS NULL OR ea.ReceivedAt < DATEADD(DAY, 1, @DateTo))
            AND (@IsInterviewerActive IS NULL OR ISNULL(ul.IsActive, 1) = @IsInterviewerActive)
    )
    SELECT
        bd.*,
        magInfo.MAGName       AS MasterAssessmentGroupName,
        magInfo.AssessmentGroupId AS ResolvedAssessmentGroupId,
        magInfo.AssessmentGroupName
    FROM BaseData bd
    OUTER APPLY (
        SELECT TOP 1 mag.MAGName, mag.AssessmentGroupId, ag.AssessmentGroup AS AssessmentGroupName
        FROM MasterAssessmentGroup mag
        LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
        WHERE LTRIM(RTRIM(LOWER(mag.MAGName))) = LTRIM(RTRIM(LOWER(bd.MagName)))
          AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
    ) magInfo
    WHERE bd.RowNum BETWEEN ((@Page - 1) * @PageSize + 1) AND (@Page * @PageSize)
    ORDER BY bd.RowNum
END
GO
