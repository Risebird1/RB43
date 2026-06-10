CREATE OR ALTER PROCEDURE [dbo].[usp_GetEligibleProspectsForAutomation]
    @BatchSize INT = 2
AS
BEGIN
    SET NOCOUNT ON;
	set @BatchSize = 3

    DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);

    -- Reset daily counter if last attempt was a prior day
    UPDATE NaukriProspectiveInterviewer
    SET PlivoCallAttemptsToday = 0
    WHERE PlivoCallLastAttemptDate IS NOT NULL
      AND PlivoCallLastAttemptDate < @Today
      AND PlivoCallAttemptsToday > 0;

    ;WITH Eligible AS (
        SELECT
            np.*,
            magInfo.AssessmentGroupId AS ResolvedAssessmentGroupId,
            magInfo.AssessmentGroupName,
            ROW_NUMBER() OVER (ORDER BY
                -- Priority 1: MAG resolved but email not yet sent — send email first
                CASE WHEN np.ApplicationLink IS NOT NULL AND np.ApplicationLink <> ''
                          AND ISNULL(np.InvitationEmailSent, 0) = 0
                     THEN 0 ELSE 1 END ASC,
                -- Priority 2: Email sent, no response, grace period elapsed — ready for call
                CASE WHEN ISNULL(np.InvitationEmailSent, 0) = 1
                          AND np.ProspectResponse IS NULL
                          AND ISNULL(np.SelfRegistrationCompleted, 0) = 0
                          AND np.InvitationEmailSentAt IS NOT NULL
                          AND DATEDIFF(MINUTE, np.InvitationEmailSentAt, GETUTCDATE()) >= 60
                          AND ISNULL(np.PlivoCallStatus, '') NOT IN ('completed', 'answered')
                     THEN 0 ELSE 1 END ASC,
                -- Priority 3: Call completed with non-interested disposition — needs cleanup
                CASE WHEN np.PlivoCallStatus IN ('completed', 'answered')
                          AND LOWER(ISNULL(np.PlivoProspectDisposition, '')) <> 'interested'
                          AND ISNULL(np.AutomationStatus, '') NOT IN ('Cold', 'ManualRequired')
                     THEN 0 ELSE 1 END ASC,
                -- Priority 4: Call completed with interested disposition — needs completion
                CASE WHEN np.PlivoCallStatus IN ('completed', 'answered')
                          AND LOWER(ISNULL(np.PlivoProspectDisposition, '')) = 'interested'
                     THEN 0 ELSE 1 END ASC,
                -- Priority 5: Positive intent confirmed (email or call) but not yet onboarded — auto-continue to acknowledgement
                CASE WHEN LOWER(ISNULL(np.Intent, '')) = 'interested'
                          AND ISNULL(np.OnboardingCompleted, 0) = 0
                     THEN 0 ELSE 1 END ASC,
                -- Then chronological
                np.ReceivedAt ASC, np.Id ASC
            ) AS RowNum
        FROM NaukriProspectiveInterviewer np
        OUTER APPLY (
            SELECT TOP 1 mag.AssessmentGroupId, ag.AssessmentGroup AS AssessmentGroupName
            FROM MasterAssessmentGroup mag
            LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
            WHERE LTRIM(RTRIM(LOWER(mag.MAGName))) = LTRIM(RTRIM(LOWER(np.MagName)))
              AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
        ) magInfo
        WHERE np.Status IN ('Open', 'InProgress')
          AND ISNULL(np.AutomationStatus, '') NOT IN (
              'Cold', 'ManualRequired', 'Completed', 'Error',
              'ParsingResume', 'ResolvingMAG', 'SendingEmail',
              -- Auto-onboarding has reached the acknowledgement stage; completion now
              -- depends on the interviewer accepting the acknowledgement email (handled in the web app).
              'AwaitingAcknowledgement'
          )
          AND ISNULL(np.PlivoCallStatus, '') NOT IN ('initiated', 'triggered')
          AND np.ResumeLink IS NOT NULL AND np.ResumeLink <> ''
          -- Don't reprocess prospects who have already been onboarded.
          AND ISNULL(np.OnboardingCompleted, 0) = 0
          -- Exclude prospects who already responded to email (no call needed) — EXCEPT
          -- those whose confirmed intent is 'Interested', which must be auto-continued through
          -- EV → AI fitment → onboarding → acknowledgement email.
          AND (
              ISNULL(np.InvitationEmailSent, 0) = 0
              OR np.ProspectResponse IS NULL
              OR LOWER(ISNULL(np.Intent, '')) = 'interested'
          )
          AND ISNULL(np.SelfRegistrationCompleted, 0) = 0
          AND (
              -- Needs resume parse
              ISNULL(np.ResumeParseCompleted, 0) = 0
              -- OR: Resume parsed, not duplicate, needs MAG link
              OR (np.ResumeParseCompleted = 1 AND ISNULL(np.IsExistingInterviewer, 0) = 0
                  AND (np.ApplicationLink IS NULL OR np.ApplicationLink = ''))
              -- OR: MAG link generated, email not yet sent
              OR (np.ApplicationLink IS NOT NULL AND np.ApplicationLink <> ''
                  AND ISNULL(np.InvitationEmailSent, 0) = 0)
              -- OR: Email sent, no response, grace period (60 min) elapsed, needs call
              OR (ISNULL(np.InvitationEmailSent, 0) = 1
                  AND np.ProspectResponse IS NULL
                  AND np.InvitationEmailSentAt IS NOT NULL
                  AND DATEDIFF(MINUTE, np.InvitationEmailSentAt, GETUTCDATE()) >= 60
                  AND ISNULL(np.PlivoCallAttemptsTotal, 0) < 5
                  AND ISNULL(np.PlivoCallAttemptsToday, 0) < 2
                  AND ISNULL(np.PlivoCallStatus, '') NOT IN ('completed', 'answered')
                  AND np.ParsedPhone IS NOT NULL AND np.ParsedPhone <> '')
              -- OR: Call completed — needs disposition handling
              OR (np.PlivoCallStatus IN ('completed', 'answered')
                  AND ISNULL(np.AutomationStatus, '') NOT IN ('Cold', 'ManualRequired', 'Completed'))
              -- OR: Positive intent confirmed (email reply or AI call) but not yet onboarded —
              --     auto-continue through EV → AI fitment → onboarding → acknowledgement email.
              OR (LOWER(ISNULL(np.Intent, '')) = 'interested'
                  AND ISNULL(np.OnboardingCompleted, 0) = 0)
          )
    )
    SELECT * FROM Eligible WHERE RowNum <= @BatchSize;
END
GO
