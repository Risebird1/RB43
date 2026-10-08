IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateBouncedCandidateEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateBouncedCandidateEmail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Fix Email on the Bounce & Dropped Mail page. Corrects a candidate's address everywhere it is stored,
-- in one transaction:
--   UserList.EmailId (login identity) and InterviewMeetings.candidateEmail for
--   open (unscheduled) and upcoming meetings only; past meetings keep the address they were held with.
-- Logs CandidateEmailChangeLog and MailBounceResolution(Status = 'EmailCorrected').
--
-- Access is checked by the caller (usp_GetBounceDroppedRowForAction) before this runs.
-- @Result: OK | DUPLICATE | NOT_FOUND | SAME
-- On DUPLICATE, @OwnerName / @OwnerProfile name the account that already uses the address (an email is a login
-- id, unique across all of UserList, not per channel), so the page can say who owns it.
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

DECLARE @UserId int, @OldEmail varchar(150);

BEGIN TRY
BEGIN TRAN;

SELECT TOP 1 @UserId = ul.id, @OldEmail = ul.EmailId
FROM Candidate ca
JOIN CandidatesMaster cm ON cm.id = ca.CandidateMasterId
JOIN UserList ul WITH (UPDLOCK, HOLDLOCK) ON ul.id = cm.UserId
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

DECLARE @OwnerId int;

-- Exact match first: an index seek, locked so nobody can take the address before we commit.
SELECT TOP 1 @OwnerId = u.id
FROM UserList u WITH (UPDLOCK, HOLDLOCK)
WHERE u.EmailId = @NewEmail AND u.id <> @UserId;

-- Also catch stored addresses with stray spaces or different case. This one cannot use an index, so it is read
-- without locks: locking a full scan would block every UserList write for the duration of the transaction.
IF (@OwnerId IS NULL)
BEGIN
    SELECT TOP 1 @OwnerId = u.id
    FROM UserList u
    WHERE LOWER(LTRIM(RTRIM(u.EmailId))) = @NewEmail AND u.id <> @UserId;
END

IF (@OwnerId IS NOT NULL)
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

UPDATE UserList SET EmailId = @NewEmail WHERE id = @UserId;

-- Open and upcoming meetings only (confirm with product; see handoff open questions).
UPDATE InterviewMeetings
SET candidateEmail = @NewEmail
WHERE candidateEmail = @OldEmail
  AND (interviewDate IS NULL OR interviewDate >= CAST(GETDATE() AS date));

INSERT INTO CandidateEmailChangeLog (UserId, CandidateId, OldEmail, NewEmail, [Source], ChangedBy, ChangedAt)
VALUES (@UserId, @CandidateId, @OldEmail, @NewEmail, 'BounceFixEmail', @ActionBy, GETDATE());

INSERT INTO MailBounceResolution (SendgridEventId, CandidateId, [Status], Note, ActionBy, ActionAt)
VALUES (@SendgridEventId, @CandidateId, 'EmailCorrected', 'Email changed from ' + @OldEmail + ' to ' + @NewEmail, @ActionBy, GETDATE());

COMMIT TRAN;
SET @Result = 'OK';
END TRY
BEGIN CATCH
    -- All or nothing: never leave UserList changed without the meetings / logs (or the reverse).
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH
END
GO
