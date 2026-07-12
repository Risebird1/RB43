/* ===========================================================================================
   FixFalseSelfRegistrationPanelRoleV9
   -------------------------------------------------------------------------------------------
   Remediates prospects that the automation's self-registration DETECTION marked "Onboarded"
   incorrectly. The old detection (usp_FindActiveUserByEmail + TryReflectSelfRegistrationAsync /
   CheckSelfRegistration) matched ANY active UserList row by email — it did not require an
   interviewer profile, and did not verify the interviewer was mapped to the CONFIRMED MAG.

   Symptoms fixed here (reported for records #13416 and #15936):
     • #13416 — email matched a PRE-EXISTING interviewer in a different MAG (Salesforce CRM Admin,
       AG 919). The prospect (confirmed AG 22, Salesforce Developer) was linked to that account and
       marked Onboarded, but was never mapped to AG 22 — so the panel shows active under the wrong
       role and never under Developer.
     • #15936 — email matched a NON-interviewer account (no InterviewerProfile). The prospect was
       linked to it and marked Onboarded even though no interviewer/panel actually exists.

   Remediation is decided per record from live data (not hard-coded per id):
     (A) If the prospect's email maps to a GENUINE active interviewer (has an InterviewerProfile):
         map that interviewer to the CONFIRMED assessment group via
         usp_MapInterviewerToAssessmentGroupChannels (append-only; adds AG membership + the AG's
         eligible skill channels, respecting each channel's Interviewer Lock). Keep them Onboarded.
     (B) Otherwise (email is NOT a genuine interviewer): the onboarding was bogus — clear the false
         self-registration / onboarded state and reset the funnel to 'MAGConfirmed' so the record
         re-enters the pipeline for correct handling.

   SAFETY: dry-run by default (@Commit = 0 → prints intended actions and ROLLS BACK). Review the
   BEFORE/AFTER output, then re-run with @Commit = 1 to persist. Idempotent: the mapping SP never
   duplicates channels, and re-running after a commit is a no-op for already-correct records.
   =========================================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit BIT = 0;                                   -- <<< set to 1 to persist
DECLARE @Actor  VARCHAR(150) = 'DataFix-FalseSelfRegV9';

DECLARE @Targets TABLE (RecordId INT PRIMARY KEY);
INSERT INTO @Targets (RecordId) VALUES (13416), (15936);   -- add more ids here if needed

-------------------------------------------------------------------------------------------------
-- BEFORE snapshot
-------------------------------------------------------------------------------------------------
SELECT 'BEFORE' AS Phase, np.Id, np.CandidateName, np.ParsedEmail, np.MagName,
       np.AssessmentGroupId AS ConfirmedAG, np.FunnelStage, np.Status, np.TicketStatus,
       np.OnboardingCompleted, np.SelfRegistrationCompleted, np.OnboardedUserId, np.OnboardedProfileId,
       np.OnboardingType
FROM NaukriProspectiveInterviewer np
JOIN @Targets t ON t.RecordId = np.Id
ORDER BY np.Id;

BEGIN TRAN;

DECLARE @RecordId INT, @Email VARCHAR(150), @AgId INT, @OldOnbUser INT,
        @IvUser INT, @IvProfile INT, @Note NVARCHAR(500);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT RecordId FROM @Targets;
OPEN cur;
FETCH NEXT FROM cur INTO @RecordId;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @Email = ParsedEmail, @AgId = AssessmentGroupId, @OldOnbUser = OnboardedUserId
    FROM NaukriProspectiveInterviewer WHERE Id = @RecordId;

    SET @IvUser = NULL; SET @IvProfile = NULL;
    SELECT TOP 1 @IvUser = ul.id, @IvProfile = ip.Id
    FROM UserList ul
    INNER JOIN InterviewerProfile ip ON ip.UserId = ul.id
    WHERE LOWER(LTRIM(RTRIM(ul.EmailId))) = LOWER(LTRIM(RTRIM(@Email)))
      AND ul.IsActive = 1;

    IF @IvUser IS NOT NULL AND @AgId IS NOT NULL AND @AgId > 0
    BEGIN
        -- (A) Genuine interviewer → map to the confirmed AG (append-only) and keep Onboarded.
        EXEC dbo.usp_MapInterviewerToAssessmentGroupChannels
            @InterviewerProfileId = @IvProfile,
            @UserId               = @IvUser,
            @AssessmentGroupId    = @AgId,
            @AssignedBy           = @Actor;

        UPDATE NaukriProspectiveInterviewer
        SET OnboardedUserId    = @IvUser,
            OnboardedProfileId = @IvProfile,
            UpdatedAt          = GETUTCDATE()
        WHERE Id = @RecordId;

        SET @Note = CONCAT(N'Remediation (A): existing interviewer (UserId ', @IvUser, N', Profile ',
                           @IvProfile, N') mapped to confirmed AG ', @AgId,
                           N' — was previously onboarded/linked without the AG mapping.');
        INSERT INTO NaukriOnboardingHistory (RecordId, EventType, FromValue, ToValue, ChangedBy, ChangedAt)
        VALUES (@RecordId, 'SkillChannelSelected', N'—', @Note, @Actor, GETUTCDATE());

        PRINT CONCAT('#', @RecordId, ' → (A) mapped UserId ', @IvUser, ' to AG ', @AgId, '.');
    END
    ELSE
    BEGIN
        -- (B) Not a genuine interviewer → revert the false onboarding; re-open at MAGConfirmed.
        UPDATE NaukriProspectiveInterviewer
        SET OnboardingCompleted       = NULL,
            SelfRegistrationCompleted = NULL,
            OnboardedUserId           = NULL,
            OnboardedProfileId        = NULL,
            OnboardingType            = NULL,
            AutomationStatus          = NULL,
            AcknowledgementEmailSent  = NULL,
            FunnelStage               = 'MAGConfirmed',
            TicketStatus              = 'InProgress',
            Status                    = 'Open',
            UpdatedAt                 = GETUTCDATE()
        WHERE Id = @RecordId;

        SET @Note = CONCAT(N'Remediation (B): reverted FALSE self-registration — email ', @Email,
                           N' does not map to a genuine interviewer (was linked to UserId ',
                           ISNULL(CONVERT(NVARCHAR(20), @OldOnbUser), N'NULL'),
                           N'). Funnel reset to MAGConfirmed for correct onboarding.');
        INSERT INTO NaukriOnboardingHistory (RecordId, EventType, FromValue, ToValue, ChangedBy, ChangedAt)
        VALUES (@RecordId, 'AutomationNote', N'—', @Note, @Actor, GETUTCDATE());

        PRINT CONCAT('#', @RecordId, ' → (B) reverted false onboarding (was UserId ',
                     ISNULL(CONVERT(VARCHAR(20), @OldOnbUser), 'NULL'), ').');
    END

    FETCH NEXT FROM cur INTO @RecordId;
END
CLOSE cur;
DEALLOCATE cur;

-------------------------------------------------------------------------------------------------
-- AFTER snapshot (within the transaction)
-------------------------------------------------------------------------------------------------
SELECT 'AFTER' AS Phase, np.Id, np.CandidateName, np.ParsedEmail, np.MagName,
       np.AssessmentGroupId AS ConfirmedAG, np.FunnelStage, np.Status, np.TicketStatus,
       np.OnboardingCompleted, np.SelfRegistrationCompleted, np.OnboardedUserId, np.OnboardedProfileId,
       np.OnboardingType
FROM NaukriProspectiveInterviewer np
JOIN @Targets t ON t.RecordId = np.Id
ORDER BY np.Id;

-- Channels now held by the (still-)linked interviewers, so you can verify the correct role mapping.
SELECT 'AFTER-Channels' AS Phase, np.Id AS RecordId, ti.UserId, ti.IRFId,
       irf.AssessmentGroupId AS IRF_AGId, ag.AssessmentGroup AS MappedRole,
       ti.IsActive, ti.IsAvailable, ti.[Source], ti.AddedAt
FROM NaukriProspectiveInterviewer np
JOIN @Targets t ON t.RecordId = np.Id
JOIN TransInterviewer ti ON ti.UserId = np.OnboardedUserId
LEFT JOIN IRF irf ON irf.id = ti.IRFId
LEFT JOIN AssessmentGroup ag ON LTRIM(RTRIM(irf.AssessmentGroupId)) = CAST(ag.id AS VARCHAR)
ORDER BY np.Id, ti.AddedAt;

IF @Commit = 1
BEGIN
    COMMIT TRAN;
    PRINT '=== COMMITTED ===';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT '=== DRY RUN — rolled back. Set @Commit = 1 to persist. ===';
END
GO
