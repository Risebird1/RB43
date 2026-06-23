IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetNaukriFunnelStageCounts')
    DROP PROCEDURE usp_GetNaukriFunnelStageCounts
GO

-- Returns the count of prospects per funnel stage, honouring the same filters as the
-- list query (including the funnel-stage filter) so the breakdown always adds up to the
-- matched total shown on the dashboard. Records with no funnel stage are reported as 'New'.
CREATE PROCEDURE [dbo].[usp_GetNaukriFunnelStageCounts]
    @Status             VARCHAR(50)  = NULL,
    @CandidateName      NVARCHAR(200)= NULL,
    @MagName            NVARCHAR(200)= NULL,
    @Source             VARCHAR(50)  = NULL,
    @AutomationStatus   VARCHAR(50)  = NULL,
    @CallStatus         VARCHAR(50)  = NULL,
    @Disposition        VARCHAR(50)  = NULL,
    @FunnelStage        VARCHAR(50)  = NULL,   -- exact stage value, or '_none_' for unstarted (New)
    @OnboardingType     VARCHAR(50)  = NULL,   -- SelfOnboarded | IntentAutoProcessed | ManuallyOnboarded | _none_
    @DateFrom           DATETIME     = NULL,
    @DateTo             DATETIME     = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CASE WHEN ISNULL(np.FunnelStage, '') = '' THEN 'New' ELSE np.FunnelStage END AS FunnelStage,
        COUNT(*) AS Cnt
    FROM NaukriProspectiveInterviewer np
    WHERE
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
            OR (@FunnelStage = '_none_' AND ISNULL(np.FunnelStage, '') = '')
            OR np.FunnelStage = @FunnelStage
        )
        AND (
            @OnboardingType IS NULL
            OR (@OnboardingType = '_none_' AND ISNULL(np.OnboardingType, '') = '')
            OR np.OnboardingType = @OnboardingType
        )
        AND (@DateFrom IS NULL OR np.ReceivedAt >= @DateFrom)
        AND (@DateTo IS NULL OR np.ReceivedAt < DATEADD(DAY, 1, @DateTo))
    GROUP BY CASE WHEN ISNULL(np.FunnelStage, '') = '' THEN 'New' ELSE np.FunnelStage END
END
GO
