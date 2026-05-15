IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetNaukriProspectiveInterviewers')
    DROP PROCEDURE usp_GetNaukriProspectiveInterviewers
GO

CREATE PROCEDURE [dbo].[usp_GetNaukriProspectiveInterviewers]
    @Status             VARCHAR(50)  = NULL,
    @CandidateName      NVARCHAR(200)= NULL,
    @MagName            NVARCHAR(200)= NULL,
    @Source             VARCHAR(50)  = NULL,
    @AutomationStatus   VARCHAR(50)  = NULL,
    @CallStatus         VARCHAR(50)  = NULL,
    @Disposition        VARCHAR(50)  = NULL,
    @DateFrom           DATETIME     = NULL,
    @DateTo             DATETIME     = NULL,
    @Page               INT          = 1,
    @PageSize           INT          = 25
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page < 1 SET @Page = 1;
    IF @PageSize < 1 SET @PageSize = 25;

    ;WITH BaseData AS (
        SELECT
            np.*,
            COUNT(*) OVER() AS TotalCount,
            ROW_NUMBER() OVER (ORDER BY np.ReceivedAt DESC, np.Id DESC) AS RowNum
        FROM NaukriProspectiveInterviewer np
        WHERE
            (@Status IS NULL OR np.Status = @Status)
            AND (@CandidateName IS NULL OR np.CandidateName LIKE '%' + @CandidateName + '%')
            AND (@MagName IS NULL OR np.MagName LIKE '%' + @MagName + '%')
            AND (@Source IS NULL OR np.[Source] = @Source)
            AND (
                @AutomationStatus IS NULL
                OR (@AutomationStatus = '_any_' AND ISNULL(np.AutomationStatus, '') <> '')
                OR (@AutomationStatus = '_none_' AND ISNULL(np.AutomationStatus, '') = '')
                OR (@AutomationStatus NOT IN ('_any_', '_none_') AND np.AutomationStatus = @AutomationStatus)
            )
            AND (@CallStatus IS NULL OR LOWER(ISNULL(np.PlivoCallStatus, '')) = LOWER(@CallStatus))
            AND (@Disposition IS NULL OR LOWER(ISNULL(np.PlivoProspectDisposition, '')) = LOWER(@Disposition))
            AND (@DateFrom IS NULL OR np.ReceivedAt >= @DateFrom)
            AND (@DateTo IS NULL OR np.ReceivedAt < DATEADD(DAY, 1, @DateTo))
    )
    SELECT
        bd.*,
        magInfo.AssessmentGroupId AS ResolvedAssessmentGroupId,
        magInfo.AssessmentGroupName
    FROM BaseData bd
    OUTER APPLY (
        SELECT TOP 1 mag.AssessmentGroupId, ag.AssessmentGroup AS AssessmentGroupName
        FROM MasterAssessmentGroup mag
        LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
        WHERE LTRIM(RTRIM(LOWER(mag.MAGName))) = LTRIM(RTRIM(LOWER(bd.MagName)))
          AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
    ) magInfo
    WHERE bd.RowNum BETWEEN ((@Page - 1) * @PageSize + 1) AND (@Page * @PageSize)
    ORDER BY bd.RowNum
END
GO
