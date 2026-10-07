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
CREATE PROCEDURE [dbo].[usp_UpdateBouncedCandidateEmail]
@CandidateId int,
@SendgridEventId int,
@NewEmail varchar(150),
@ActionBy varchar(150),
@Result varchar(50) OUTPUT
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

IF EXISTS (SELECT 1 FROM UserList WITH (UPDLOCK, HOLDLOCK) WHERE EmailId = @NewEmail AND id <> @UserId)
BEGIN
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
