IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetNaukriProspectiveInterviewers')
    DROP PROCEDURE usp_GetNaukriProspectiveInterviewers
GO

CREATE PROCEDURE [dbo].[usp_GetNaukriProspectiveInterviewers]
    @Status         VARCHAR(50)  = NULL,
    @CandidateName  NVARCHAR(200)= NULL,
    @MagName        NVARCHAR(200)= NULL,
    @DateFrom       DATETIME     = NULL,
    @DateTo         DATETIME     = NULL,
    @Page           INT          = 1,
    @PageSize       INT          = 25
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        np.*,
        mag.AssessmentGroupId AS ResolvedAssessmentGroupId,
        ag.AssessmentGroup    AS AssessmentGroupName,
        COUNT(*) OVER()       AS TotalCount
    FROM NaukriProspectiveInterviewer np
    LEFT JOIN MasterAssessmentGroup mag
        ON LTRIM(RTRIM(LOWER(mag.MAGName))) = LTRIM(RTRIM(LOWER(np.MagName)))
        AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
    LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
    WHERE
        (@Status IS NULL OR np.Status = @Status)
        AND (@CandidateName IS NULL OR np.CandidateName LIKE '%' + @CandidateName + '%')
        AND (@MagName IS NULL OR np.MagName LIKE '%' + @MagName + '%')
        AND (@DateFrom IS NULL OR np.ReceivedAt >= @DateFrom)
        AND (@DateTo IS NULL OR np.ReceivedAt < DATEADD(DAY, 1, @DateTo))
    ORDER BY np.ReceivedAt DESC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO
