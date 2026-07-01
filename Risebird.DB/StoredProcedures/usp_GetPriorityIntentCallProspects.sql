-- Returns prospects to call for the PRIORITY intent-collection push.
--
-- Selection criteria:
--   * Prospect is mapped to one of today's active priority MAGs (matched either by the
--     resolved AssessmentGroupId or by tolerant MAG-name comparison).
--   * Prospect is parked at the InterestCollection funnel stage (invitation sent, intent
--     not yet captured) — i.e. their intent is still unknown.
--   * A phone number is on file and no call is currently in flight.
--
-- Crucially this procedure DOES NOT apply the usual anti-spam caps (5 total / 2 per day);
-- the only throttle is a short between-call cooldown (@CooldownMinutes) so we don't redial a
-- prospect while a just-triggered call is still ringing. The cooldown is tracked via the
-- dedicated PriorityCallLastAt column, independent of the normal PlivoCall* counters.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetPriorityIntentCallProspects]
    @BatchSize       INT = 5,
    @CooldownMinutes INT = 15
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);

    ;WITH Priority AS (
        SELECT AssessmentGroupId, MagName
        FROM   NaukriPriorityMag
        WHERE  PriorityDate = @Today AND IsActive = 1
    ),
    Eligible AS (
        SELECT
            np.*,
            magInfo.AssessmentGroupId AS ResolvedAssessmentGroupId,
            magInfo.AssessmentGroupName,
            ROW_NUMBER() OVER (ORDER BY
                -- Least-recently-attempted first so calls spread evenly across the cohort.
                ISNULL(np.PriorityCallLastAt, '2000-01-01') ASC,
                np.InvitationEmailSentAt ASC,
                np.Id ASC
            ) AS RowNum
        FROM NaukriProspectiveInterviewer np
        OUTER APPLY (
            -- Tolerant MAG-name resolution (case + invisible/odd whitespace folded by
            -- dbo.fn_NormalizeMagName), with a fallback to a direct AssessmentGroup name match.
            SELECT TOP 1 x.AssessmentGroupId, x.AssessmentGroupName
            FROM (
                SELECT 1 AS Pref, mag.AssessmentGroupId, ag.AssessmentGroup AS AssessmentGroupName
                FROM MasterAssessmentGroup mag
                LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
                WHERE dbo.fn_NormalizeMagName(mag.MAGName) = dbo.fn_NormalizeMagName(np.MagName)
                  AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
                UNION ALL
                SELECT 2 AS Pref, ag2.id AS AssessmentGroupId, ag2.AssessmentGroup AS AssessmentGroupName
                FROM AssessmentGroup ag2
                WHERE dbo.fn_NormalizeMagName(ag2.AssessmentGroup) = dbo.fn_NormalizeMagName(np.MagName)
            ) x
            WHERE x.AssessmentGroupId IS NOT NULL
            ORDER BY x.Pref
        ) magInfo
        WHERE
            -- Stuck at intent collection: invitation already sent, intent not yet captured.
            dbo.fn_NaukriCanonicalFunnelStage(np.FunnelStage) = 'InterestCollection'
            AND ISNULL(np.InvitationEmailSent, 0) = 1
            AND np.Intent IS NULL
            AND np.Status IN ('Open', 'InProgress')
            AND ISNULL(np.SelfRegistrationCompleted, 0) = 0
            AND ISNULL(np.OnboardingCompleted, 0) = 0
            AND np.ParsedPhone IS NOT NULL AND np.ParsedPhone <> ''
            -- No call currently in flight.
            AND ISNULL(np.PlivoCallStatus, '') NOT IN ('initiated', 'triggered')
            -- Short between-call cooldown (NOT the 5/2 anti-spam cap).
            AND (np.PriorityCallLastAt IS NULL
                 OR DATEDIFF(MINUTE, np.PriorityCallLastAt, GETUTCDATE()) >= @CooldownMinutes)
            -- Mapped to one of today's priority MAGs.
            AND (
                EXISTS (SELECT 1 FROM Priority pr WHERE pr.AssessmentGroupId = magInfo.AssessmentGroupId)
                OR EXISTS (SELECT 1 FROM Priority pr
                           WHERE dbo.fn_NormalizeMagName(pr.MagName) = dbo.fn_NormalizeMagName(np.MagName))
            )
    )
    SELECT * FROM Eligible WHERE RowNum <= @BatchSize;
END
GO
