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
    @FunnelStage        VARCHAR(50)  = NULL,   -- exact stage value, or '_none_' for unstarted (New)
    @OnboardingType     VARCHAR(50)  = NULL,   -- SelfOnboarded | IntentAutoProcessed | ManuallyOnboarded | _none_
    @AssignedTo         VARCHAR(150) = NULL,   -- assignee email, or '_unassigned_' for records with no assignee
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
            -- Global status breakdown over the matched set. 'Onboarded' (a FunnelStage outcome) is
            -- carved out first so the four buckets partition the total exactly:
            --   Open + InProgress + Closed + Onboarded = TotalCount.
            SUM(CASE WHEN ISNULL(np.FunnelStage, '') = 'Onboarded' THEN 1 ELSE 0 END) OVER() AS OnboardedCount,
            SUM(CASE WHEN ISNULL(np.FunnelStage, '') <> 'Onboarded'
                      AND ISNULL(np.TicketStatus, 'Open') = 'Open'                  THEN 1 ELSE 0 END) OVER() AS OpenCount,
            SUM(CASE WHEN ISNULL(np.FunnelStage, '') <> 'Onboarded'
                      AND ISNULL(np.TicketStatus, 'Open') NOT IN ('Open', 'Closed') THEN 1 ELSE 0 END) OVER() AS InProgressCount,
            SUM(CASE WHEN ISNULL(np.FunnelStage, '') <> 'Onboarded'
                      AND ISNULL(np.TicketStatus, 'Open') = 'Closed'                THEN 1 ELSE 0 END) OVER() AS ClosedCount,
            ROW_NUMBER() OVER (ORDER BY np.ReceivedAt DESC, np.Id DESC) AS RowNum
        FROM NaukriProspectiveInterviewer np
        WHERE
            -- @Status now filters on the operational TicketStatus dimension (Open | InProgress | Closed).
            (@Status IS NULL OR ISNULL(np.TicketStatus, 'Open') = @Status)
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
            AND (
                @FunnelStage IS NULL
                OR (@FunnelStage = '_none_' AND dbo.fn_NaukriCanonicalFunnelStage(np.FunnelStage) = 'New')
                OR dbo.fn_NaukriCanonicalFunnelStage(np.FunnelStage) = @FunnelStage
            )
            AND (
                @OnboardingType IS NULL
                OR (@OnboardingType = '_none_' AND ISNULL(np.OnboardingType, '') = '')
                OR np.OnboardingType = @OnboardingType
            )
            AND (
                @AssignedTo IS NULL
                OR (@AssignedTo = '_unassigned_' AND np.AssignedToUserId IS NULL)
                OR (@AssignedTo NOT IN ('_unassigned_') AND np.AssignedToEmail = @AssignedTo)
            )
            AND (@DateFrom IS NULL OR np.ReceivedAt >= @DateFrom)
            AND (@DateTo IS NULL OR np.ReceivedAt < DATEADD(DAY, 1, @DateTo))
    )
    SELECT
        bd.*,
        magInfo.AssessmentGroupId AS ResolvedAssessmentGroupId,
        magInfo.AssessmentGroupName
    FROM BaseData bd
    OUTER APPLY (
        -- Tolerant MAG-name resolution (case + invisible/odd whitespace folded by
        -- dbo.fn_NormalizeMagName), with a fallback to a direct AssessmentGroup name match.
        SELECT TOP 1 x.AssessmentGroupId, x.AssessmentGroupName
        FROM (
            SELECT 1 AS Pref, mag.AssessmentGroupId, ag.AssessmentGroup AS AssessmentGroupName
            FROM MasterAssessmentGroup mag
            LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
            WHERE dbo.fn_NormalizeMagName(mag.MAGName) = dbo.fn_NormalizeMagName(bd.MagName)
              AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
            UNION ALL
            SELECT 2 AS Pref, ag2.id AS AssessmentGroupId, ag2.AssessmentGroup AS AssessmentGroupName
            FROM AssessmentGroup ag2
            WHERE dbo.fn_NormalizeMagName(ag2.AssessmentGroup) = dbo.fn_NormalizeMagName(bd.MagName)
        ) x
        WHERE x.AssessmentGroupId IS NOT NULL
        ORDER BY x.Pref
    ) magInfo
    WHERE bd.RowNum BETWEEN ((@Page - 1) * @PageSize + 1) AND (@Page * @PageSize)
    ORDER BY bd.RowNum
END
GO
