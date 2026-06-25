                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetNaukriFunnelReachCounts')
    DROP PROCEDURE usp_GetNaukriFunnelReachCounts
GO

-- ============================================================================================
-- "Reached at least" funnel for NaukriOnboarding.
--
-- Unlike usp_GetNaukriFunnelStageCounts (which reports each record at its single CURRENT stage),
-- this proc reconstructs how far each record has EVER progressed, so the dashboard can show a
-- true descending funnel. Because FunnelStage only stores the current position (and the backfill
-- left no per-stage history), "ever reached" is derived from DURABLE signals that do not reset
-- when a record later branches off (Duplicate / NotInterested / Cold / NotQualified).
--
-- A record's ReachedRank = MAX of:
--   * the rank implied by any durable signal that is set,
--   * the rank of its current (happy-path) FunnelStage,
--   * a minimum-reach credit for records that have branched off (a brancher must have passed
--     through the stages up to its exit point).
--
-- Output rows (MetricKey, Cnt):
--   EmailParsed..Onboarded  -> CUMULATIVE count of records with ReachedRank >= that stage's rank
--   Duplicate/NotInterested/Cold/NotQualified -> current count in each terminal branch
--   NotStarted              -> records with no evidence of having entered the pipeline
--   Total                   -> all matched records
--
-- All filters from the list/counts queries are honoured EXCEPT @FunnelStage: the reach funnel is
-- a pipeline-shape overview, so selecting a single stage chip must not collapse it.
-- ============================================================================================
CREATE PROCEDURE [dbo].[usp_GetNaukriFunnelReachCounts]
    @Status             VARCHAR(50)  = NULL,
    @CandidateName      NVARCHAR(200)= NULL,
    @MagName            NVARCHAR(200)= NULL,
    @Source             VARCHAR(50)  = NULL,
    @AutomationStatus   VARCHAR(50)  = NULL,
    @CallStatus         VARCHAR(50)  = NULL,
    @Disposition        VARCHAR(50)  = NULL,
    @FunnelStage        VARCHAR(50)  = NULL,   -- accepted for signature symmetry; intentionally NOT applied
    @OnboardingType     VARCHAR(50)  = NULL,
    @DateFrom           DATETIME     = NULL,
    @DateTo             DATETIME     = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Filtered AS (
        SELECT
            np.Id,
            dbo.fn_NaukriCanonicalFunnelStage(np.FunnelStage) AS CanonStage,
            np.ParsedEmail, np.ParsedPhone, np.ResumeParseCompleted,
            np.IsExistingInterviewer,
            np.AssessmentGroupId, np.SelectedIRFId, np.ApplicationLink,
            np.InvitationEmailSent, np.InvitationEmailSentAt,
            np.Intent, np.ProspectResponse, np.IntentConfirmedAt,
            np.EmploymentVerified, np.ResumeAIAnalysisCompleted,
            np.AcknowledgementEmailSent, np.OnboardingCompleted, np.OnboardedUserId,
            np.FunnelStage
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
                @OnboardingType IS NULL
                OR (@OnboardingType = '_none_' AND ISNULL(np.OnboardingType, '') = '')
                OR np.OnboardingType = @OnboardingType
            )
            AND (@DateFrom IS NULL OR np.ReceivedAt >= @DateFrom)
            AND (@DateTo IS NULL OR np.ReceivedAt < DATEADD(DAY, 1, @DateTo))
    ),
    Reached AS (
        SELECT
            f.Id,
            f.CanonStage,
            (
                SELECT MAX(r) FROM (VALUES
                    -- 1 EmailParsed
                    (CASE WHEN (f.ParsedEmail IS NOT NULL AND LTRIM(RTRIM(f.ParsedEmail)) <> '')
                               OR f.ResumeParseCompleted = 1 THEN 1 ELSE 0 END),
                    -- 2 PhoneParsed
                    (CASE WHEN (f.ParsedPhone IS NOT NULL AND LTRIM(RTRIM(f.ParsedPhone)) <> '')
                               OR f.ResumeParseCompleted = 1 THEN 2 ELSE 0 END),
                    -- 3 DuplicateChecked (the dedup verdict was computed)
                    (CASE WHEN f.IsExistingInterviewer IS NOT NULL THEN 3 ELSE 0 END),
                    -- 4 MAGConfirmed (genuine MAG/IRF/application signal)
                    (CASE WHEN f.AssessmentGroupId IS NOT NULL
                               OR f.SelectedIRFId IS NOT NULL
                               OR (f.ApplicationLink IS NOT NULL AND LTRIM(RTRIM(f.ApplicationLink)) <> '')
                          THEN 4 ELSE 0 END),
                    -- 5 InterestCollection (invitation went out)
                    (CASE WHEN f.InvitationEmailSent = 1 OR f.InvitationEmailSentAt IS NOT NULL
                          THEN 5 ELSE 0 END),
                    -- 6 IntentConfirmed (positive intent recorded)
                    (CASE WHEN f.IntentConfirmedAt IS NOT NULL
                               AND f.CanonStage NOT IN ('NotInterested','Cold','Duplicate')
                          THEN 6 ELSE 0 END),
                    -- 7 EmploymentVerified (passed)
                    (CASE WHEN f.EmploymentVerified = 1 THEN 7 ELSE 0 END),
                    -- 8 ResumeFitment (AI résumé analysis completed)
                    (CASE WHEN f.ResumeAIAnalysisCompleted = 1 THEN 8 ELSE 0 END),
                    -- 9 AcknowledgementSent
                    (CASE WHEN f.AcknowledgementEmailSent = 1 THEN 9 ELSE 0 END),
                    -- 10 Onboarded — ONLY a genuine acceptance counts. OnboardingCompleted /
                    --    OnboardedUserId are set when the InterviewerProfile is CREATED (at
                    --    acknowledgement-send time), NOT when the prospect accepts, so they must
                    --    NOT be used here. The authoritative "accepted & onboarded" marker is
                    --    FunnelStage = 'Onboarded' (set only on acceptance / self-registration),
                    --    which is already contributed below via fn_NaukriFunnelStageRank.
                    (CASE WHEN f.CanonStage = 'Onboarded' THEN 10 ELSE 0 END),
                    -- current happy-path stage rank (0 for terminal/branch/New)
                    (dbo.fn_NaukriFunnelStageRank(f.CanonStage)),
                    -- minimum reach credited to records that have branched off
                    (CASE f.CanonStage
                         WHEN 'Duplicate'     THEN 3
                         WHEN 'NotInterested' THEN 5
                         WHEN 'Cold'          THEN 5
                         WHEN 'NotQualified'  THEN 6
                         ELSE 0 END)
                ) AS v(r)
            ) AS ReachedRank
        FROM Filtered f
    )
    SELECT MetricKey, Cnt FROM (
        SELECT 'EmailParsed'         AS MetricKey, SUM(CASE WHEN ReachedRank >= 1  THEN 1 ELSE 0 END) AS Cnt FROM Reached
        UNION ALL SELECT 'PhoneParsed',         SUM(CASE WHEN ReachedRank >= 2  THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'DuplicateChecked',    SUM(CASE WHEN ReachedRank >= 3  THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'MAGConfirmed',        SUM(CASE WHEN ReachedRank >= 4  THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'InterestCollection',  SUM(CASE WHEN ReachedRank >= 5  THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'IntentConfirmed',     SUM(CASE WHEN ReachedRank >= 6  THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'EmploymentVerified',  SUM(CASE WHEN ReachedRank >= 7  THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'ResumeFitment',       SUM(CASE WHEN ReachedRank >= 8  THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'AcknowledgementSent', SUM(CASE WHEN ReachedRank >= 9  THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'Onboarded',           SUM(CASE WHEN ReachedRank >= 10 THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'Duplicate',           SUM(CASE WHEN CanonStage = 'Duplicate'     THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'NotInterested',       SUM(CASE WHEN CanonStage = 'NotInterested' THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'Cold',                SUM(CASE WHEN CanonStage = 'Cold'          THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'NotQualified',        SUM(CASE WHEN CanonStage = 'NotQualified'  THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'NotStarted',          SUM(CASE WHEN ReachedRank = 0 THEN 1 ELSE 0 END) FROM Reached
        UNION ALL SELECT 'Total',               COUNT(*) FROM Reached
    ) x
END
GO
