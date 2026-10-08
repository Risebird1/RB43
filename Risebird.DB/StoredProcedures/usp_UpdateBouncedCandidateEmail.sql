IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateBouncedCandidateEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateBouncedCandidateEmail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Fix Email on the Bounce & Dropped Mail page, in one transaction.
--
-- An email may belong to a candidate in any number of channels (one UserList login -> one CandidatesMaster ->
-- one Candidate row per channel; usp_AddCandidate reuses the candidate login of an existing email). So the new
-- address is only refused when it already belongs to a candidate IN THIS CHANNEL:
--
--   New address belongs to nobody, or only to non-candidate users (interviewer, HR, ...):
--       rename this candidate's login (UserList.EmailId)                                   -> @Result = 'OK'
--   New address belongs to a candidate who is NOT in this channel:
--       re-link this channel's Candidate row to that candidate's CandidatesMaster, and move this channel's
--       per-candidate rows (status history, audio, evaluator reports, HR evaluator) with it -> @Result = 'RELINKED'
--       The old login keeps its other channels untouched.
--   New address belongs to a candidate who IS already in this channel                       -> @Result = 'DUPLICATE'
--       (@OwnerName / @OwnerProfile say who, so the page can show it)
--
-- In every success case: InterviewMeetings.candidateEmail moves to the new address for open (unscheduled) and
-- upcoming meetings (for a re-link: of this channel only; past meetings keep the address they were held with),
-- and CandidateEmailChangeLog + MailBounceResolution(Status = 'EmailCorrected') are written.
--
-- Access is checked by the caller (usp_GetBounceDroppedRowForAction) before this runs.
-- @Result: OK | RELINKED | DUPLICATE | NOT_FOUND | SAME
CREATE PROCEDURE [dbo].[usp_UpdateBouncedCandidateEmail]
@CandidateId int,
@SendgridEventId int,
@NewEmail varchar(150),
@ActionBy varchar(150),
@Result varchar(50) OUTPUT,
@OwnerName varchar(300) = NULL OUTPUT,
@OwnerProfile varchar(100) = NULL OUTPUT
AS
BEGIN
SET NOCOUNT ON;
SET XACT_ABORT ON;

SET @NewEmail = LOWER(LTRIM(RTRIM(@NewEmail)));

DECLARE @UserId int, @OldEmail varchar(150), @OldMasterId int, @IRFId int, @IRFName varchar(200);
DECLARE @CandidateProfileId int = (SELECT TOP 1 id FROM [Profile] WHERE [Name] = 'Candidate');
IF (@CandidateProfileId IS NULL) SET @CandidateProfileId = 23;

BEGIN TRY
BEGIN TRAN;

SELECT TOP 1 @UserId = ul.id, @OldEmail = ul.EmailId, @OldMasterId = cm.id, @IRFId = ca.IRFId, @IRFName = irf.IRFId
FROM Candidate ca WITH (UPDLOCK, HOLDLOCK)
JOIN CandidatesMaster cm ON cm.id = ca.CandidateMasterId
JOIN UserList ul WITH (UPDLOCK, HOLDLOCK) ON ul.id = cm.UserId
LEFT JOIN IRF irf ON irf.id = ca.IRFId
WHERE ca.id = @CandidateId;

IF (@UserId IS NULL)
BEGIN
    ROLLBACK TRAN;
    SET @Result = 'NOT_FOUND';
    RETURN;
END

IF (LOWER(LTRIM(RTRIM(@OldEmail))) = @NewEmail)
BEGIN
    ROLLBACK TRAN;
    SET @Result = 'SAME';
    RETURN;
END

-- Is the new address already a CANDIDATE login? Exact match first (index seek, locked so nobody can take the
-- address before we commit); then stored values with stray spaces / different case, read without locks because
-- that one is a full scan and locking it would block every UserList write for the duration of the transaction.
DECLARE @OwnerId int;

SELECT TOP 1 @OwnerId = u.id
FROM UserList u WITH (UPDLOCK, HOLDLOCK)
WHERE u.EmailId = @NewEmail AND u.id <> @UserId AND u.idProfile = @CandidateProfileId
ORDER BY u.id;

IF (@OwnerId IS NULL)
BEGIN
    SELECT TOP 1 @OwnerId = u.id
    FROM UserList u
    WHERE LOWER(LTRIM(RTRIM(u.EmailId))) = @NewEmail AND u.id <> @UserId AND u.idProfile = @CandidateProfileId
    ORDER BY u.id;
END

IF (@OwnerId IS NULL)
BEGIN
    ---------------------------------------------------------------------------------------------------------------
    -- Not a candidate anywhere: rename this candidate's login.
    ---------------------------------------------------------------------------------------------------------------
    UPDATE UserList SET EmailId = @NewEmail WHERE id = @UserId;

    -- Open and upcoming meetings only (confirm with product; see handoff open questions).
    UPDATE InterviewMeetings
    SET candidateEmail = @NewEmail
    WHERE candidateEmail = @OldEmail
      AND (interviewDate IS NULL OR interviewDate >= CAST(GETDATE() AS date));

    INSERT INTO CandidateEmailChangeLog (UserId, CandidateId, OldEmail, NewEmail, [Source], ChangedBy, ChangedAt)
    VALUES (@UserId, @CandidateId, @OldEmail, @NewEmail, 'BounceFixEmail', @ActionBy, GETDATE());

    SET @Result = 'OK';
END
ELSE
BEGIN
    ---------------------------------------------------------------------------------------------------------------
    -- The address is another candidate's login. Refuse only when that candidate is already in this channel.
    ---------------------------------------------------------------------------------------------------------------
    DECLARE @NewMasterId int = (SELECT TOP 1 id FROM CandidatesMaster WITH (UPDLOCK, HOLDLOCK) WHERE UserId = @OwnerId ORDER BY id);

    IF (@NewMasterId IS NOT NULL AND EXISTS (
            SELECT 1 FROM Candidate c
            WHERE c.CandidateMasterId = @NewMasterId AND c.IRFId = @IRFId AND c.id <> @CandidateId))
    BEGIN
        SELECT @OwnerName = LTRIM(RTRIM(ISNULL(u.FirstName, '') + ' ' + ISNULL(u.LastName, ''))),
               @OwnerProfile = p.Name
        FROM UserList u
        LEFT JOIN Profile p ON p.id = u.idProfile
        WHERE u.id = @OwnerId;

        ROLLBACK TRAN;
        SET @Result = 'DUPLICATE';
        RETURN;
    END

    -- Same as usp_AddCandidate: a candidate login without a CandidatesMaster row gets one.
    IF (@NewMasterId IS NULL)
    BEGIN
        INSERT INTO CandidatesMaster (UserId) VALUES (@OwnerId);
        SET @NewMasterId = SCOPE_IDENTITY();
    END

    -- Re-link this channel's candidate to the existing person, with this channel's per-candidate data.
    UPDATE Candidate SET CandidateMasterId = @NewMasterId WHERE id = @CandidateId;

    UPDATE CandidateStatus             SET CandidateId = @NewMasterId WHERE CandidateId = @OldMasterId AND IRFId = @IRFId;
    UPDATE CandidateAudio              SET CandidateId = @NewMasterId WHERE CandidateId = @OldMasterId AND IRFId = @IRFId;
    UPDATE JoinabilityReport           SET CandidateId = @NewMasterId WHERE CandidateId = @OldMasterId AND IRFId = @IRFId;
    UPDATE SelectabilityReport         SET CandidateId = @NewMasterId WHERE CandidateId = @OldMasterId AND IRFId = @IRFId;
    UPDATE SkillGeneralEvaluatorReport SET CandidateId = @NewMasterId WHERE CandidateId = @OldMasterId AND IRFId = @IRFId;
    UPDATE TechSkillEvaluatorReport    SET CandidateId = @NewMasterId WHERE CandidateId = @OldMasterId AND IRFId = @IRFId;
    UPDATE TransHREvaluatorIRF         SET CandidateId = @NewMasterId WHERE CandidateId = @OldMasterId AND IRFId = @IRFId;

    -- Open and upcoming meetings of THIS channel only: the old login may still be a candidate elsewhere.
    UPDATE InterviewMeetings
    SET candidateEmail = @NewEmail
    WHERE candidateEmail = @OldEmail
      AND IRFId = @IRFName
      AND (interviewDate IS NULL OR interviewDate >= CAST(GETDATE() AS date));

    -- Logged against the login the candidate now uses, so bounces on the old address still find this candidate.
    INSERT INTO CandidateEmailChangeLog (UserId, CandidateId, OldEmail, NewEmail, [Source], ChangedBy, ChangedAt)
    VALUES (@OwnerId, @CandidateId, @OldEmail, @NewEmail, 'BounceFixEmailRelink', @ActionBy, GETDATE());

    SET @Result = 'RELINKED';
END

INSERT INTO MailBounceResolution (SendgridEventId, CandidateId, [Status], Note, ActionBy, ActionAt)
VALUES (@SendgridEventId, @CandidateId, 'EmailCorrected',
        CASE WHEN @Result = 'RELINKED'
             THEN 'Linked to the existing candidate ' + @NewEmail + ' (was ' + @OldEmail + ')'
             ELSE 'Email changed from ' + @OldEmail + ' to ' + @NewEmail END,
        @ActionBy, GETDATE());

COMMIT TRAN;
END TRY
BEGIN CATCH
    -- All or nothing: never leave UserList / Candidate changed without the meetings / logs (or the reverse).
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    SET @Result = NULL;
    THROW;
END CATCH
END
GO
