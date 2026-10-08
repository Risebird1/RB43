IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_NextInterviewInviteSequence'))
BEGIN
    DROP PROCEDURE dbo.usp_NextInterviewInviteSequence
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Bounce & Dropped – QA round 1 (Q4): next calendar SEQUENCE for an interview's invite.
-- Called before every interview .ics is sent (invite, reschedule, bounce resend, cancel), so the number only grows
-- and calendars treat the newest mail as the current version of the one event (UID interview-{id}@risebird.io).
-- Returns the new value (first call = 1). Run Tables/InterviewInviteSequence.sql first.
CREATE PROCEDURE [dbo].[usp_NextInterviewInviteSequence]
@InterviewId int
AS
BEGIN
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @next TABLE (Sequence int);

BEGIN TRAN;

UPDATE InterviewInviteSequence WITH (UPDLOCK, HOLDLOCK)
SET Sequence = Sequence + 1, UpdatedAt = GETDATE()
OUTPUT inserted.Sequence INTO @next
WHERE InterviewId = @InterviewId;

IF NOT EXISTS (SELECT 1 FROM @next)
BEGIN
    INSERT INTO InterviewInviteSequence (InterviewId, Sequence, UpdatedAt)
    OUTPUT inserted.Sequence INTO @next
    VALUES (@InterviewId, 1, GETDATE());
END

COMMIT TRAN;

SELECT TOP 1 Sequence FROM @next;
END
GO
