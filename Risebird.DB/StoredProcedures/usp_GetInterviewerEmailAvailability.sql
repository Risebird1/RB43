IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetInterviewerEmailAvailability')
    DROP PROCEDURE usp_GetInterviewerEmailAvailability
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerEmailAvailability]
    @Status         VARCHAR(50)  = NULL,
    @SenderEmail    VARCHAR(150) = NULL,
    @ResumeFitment  VARCHAR(50)  = NULL,
    @DateFrom       DATETIME     = NULL,
    @DateTo         DATETIME     = NULL,
    @Source         VARCHAR(50)  = NULL,
    @IsAutoClosed   BIT          = NULL,
    @Page           INT          = 1,
    @PageSize       INT          = 25
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
            COUNT(*) OVER()       AS TotalCount,
            SUM(CASE WHEN ea.Status = 'Open'       THEN 1 ELSE 0 END) OVER() AS OpenCount,
            SUM(CASE WHEN ea.Status = 'InProgress'  THEN 1 ELSE 0 END) OVER() AS InProgressCount,
            SUM(CASE WHEN ea.Status = 'Verified'    THEN 1 ELSE 0 END) OVER() AS VerifiedCount,
            SUM(CASE WHEN ea.Status = 'Closed'      THEN 1 ELSE 0 END) OVER() AS ClosedCount,
            ROW_NUMBER() OVER (ORDER BY ea.ReceivedAt DESC, ea.Id DESC) AS RowNum
        FROM InterviewerEmailAvailability ea
        LEFT JOIN InterviewerProfile ip ON ip.Id = ea.InterviewerProfileId
        LEFT JOIN UserList ul ON ul.id = ea.UserId
        WHERE
            (@Status IS NULL OR ea.Status = @Status)
            AND (@SenderEmail IS NULL OR ea.SenderEmail LIKE '%' + @SenderEmail + '%')
            AND (@ResumeFitment IS NULL OR ea.ResumeFitment = @ResumeFitment)
            AND (@Source IS NULL OR ea.Source LIKE @Source + '%')
            AND (@IsAutoClosed IS NULL OR ISNULL(ea.IsAutoClosed, 0) = @IsAutoClosed)
            AND (@DateFrom IS NULL OR ea.ReceivedAt >= @DateFrom)
            AND (@DateTo IS NULL OR ea.ReceivedAt < DATEADD(DAY, 1, @DateTo))
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
