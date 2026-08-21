IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AssignAIQuestionSetToInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_AssignAIQuestionSetToInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Binds an interview to exactly one question set, once, for the interview's whole life.
--
-- Idempotence rests on two things that must stay together: the unique index on
-- InterviewAIQuestionSet.InterviewId, and the UPDLOCK, HOLDLOCK re-read below. The re-read takes a
-- range lock on that key even when no row exists yet, which is what stops two simultaneous
-- launches for the same interview from each inserting a different set. Remove either one and the
-- race comes back.
--
-- Repeat calls return the same set, so reconnect and resume are safe.
--
-- Three outputs, and the caller needs all three to tell four different situations apart:
--   @SetsEnabled     IRF.UseAIQuestionSets -- the channel's explicit opt-in.
--   @ChannelHasSets  whether any set rows exist for the channel. Purely factual, independent of
--                    the flag, so the caller can warn about a channel that has papers it is not
--                    using and refuse to serve one that is switched on but has none.
--   @SetId           the bound set, or NULL.
-- Enabled with no sets is a misconfiguration the caller must fail closed on rather than quietly
-- falling back to the whole channel pool.
--
-- An existing binding always wins, checked before the flag is consulted. Switching a channel off
-- must never move a candidate who is mid-interview onto a different question order: an interview is
-- locked to its paper for life, so the flag only decides whether a NEW binding is created.
Create PROCEDURE [dbo].[usp_AssignAIQuestionSetToInterview]
(
    @InterviewId int,
    @SetId int OUTPUT,
    @ChannelHasSets bit OUTPUT,
    @SetsEnabled bit OUTPUT,
    @Message nvarchar(400) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET @SetId = NULL;
    SET @ChannelHasSets = 0;
    SET @SetsEnabled = 0;
    SET @Message = NULL;

    -- Safe to call from inside another transaction: a bare ROLLBACK would discard the caller's work
    -- too, so when a transaction is already open we scope our own undo to a savepoint. This matters
    -- because booking flows may grow an ambient transaction around the call.
    DECLARE @NestedTran bit = CASE WHEN @@TRANCOUNT > 0 THEN 1 ELSE 0 END;

 begin try
  IF @NestedTran = 1 SAVE TRANSACTION AssignAIQSet; ELSE begin tran;

    -- InterviewMeetings.IRFId is a varchar(200) holding the IRF business code, not IRF.id.
    -- Resolving a channel through IRF.IRFId = im.IRFId is the existing convention in this
    -- database; it is not a bug to be "fixed" into an integer join.
    DECLARE @IRFIntId int, @IsPreset bit = 0, @UseSets bit = 0;

    SELECT @IRFIntId = irf.id,
           @IsPreset = CASE WHEN ISNULL(irf.IsAIInterview, 0) = 1 AND ISNULL(irf.AIInterviewType, 0) = 2
                            THEN 1 ELSE 0 END,
           @UseSets = CASE WHEN ISNULL(irf.UseAIQuestionSets, 0) = 1 THEN 1 ELSE 0 END
    FROM InterviewMeetings im
    JOIN IRF irf ON irf.IRFId = im.IRFId
    WHERE im.id = @InterviewId;

    IF @IRFIntId IS NULL
    BEGIN
        SET @Message = 'Interview not found.';
        IF @NestedTran = 0 commit tran;
        RETURN;
    END

    IF @IsPreset = 0
    BEGIN
        SET @Message = 'This interview is not on a preset AI interview channel.';
        IF @NestedTran = 0 commit tran;
        RETURN;
    END

    SET @SetsEnabled = @UseSets;

    -- Early exit for a channel with no papers at all, before any locking, so channels that have
    -- not adopted the feature behave exactly as they did before it existed.
    IF NOT EXISTS (SELECT 1 FROM AIQuestionSets WHERE IRFId = @IRFIntId)
    BEGIN
        SET @Message = CASE WHEN @UseSets = 1
                            THEN 'This channel is switched on for question sets but none have been created.'
                            ELSE 'This channel has no question sets.' END;
        IF @NestedTran = 0 commit tran;
        RETURN;
    END

    SET @ChannelHasSets = 1;

    -- An interview already bound keeps its paper whatever the flag now says.
    SELECT @SetId = ias.SetId
    FROM InterviewAIQuestionSet ias WITH (UPDLOCK, HOLDLOCK)
    WHERE ias.InterviewId = @InterviewId;

    IF @SetId IS NOT NULL
    BEGIN
        SET @Message = 'Already bound.';
        IF @NestedTran = 0 commit tran;
        RETURN;
    END

    -- Not bound yet, so the channel's opt-in decides whether it ever will be.
    IF @UseSets = 0
    BEGIN
        SET @Message = 'This channel has question sets but is not switched on to use them, so the standard question order applies.';
        IF @NestedTran = 0 commit tran;
        RETURN;
    END

    -- Spread papers across a batch: take the least-used set, oldest first on a tie.
    SELECT TOP 1 @SetId = s.id
    FROM AIQuestionSets s
    OUTER APPLY (SELECT COUNT(*) AS Assigned
                 FROM InterviewAIQuestionSet x
                 WHERE x.SetId = s.id) a
    WHERE s.IRFId = @IRFIntId
    ORDER BY a.Assigned ASC, s.id ASC;

    IF @SetId IS NULL
    BEGIN
        SET @Message = 'No question set could be selected for this channel.';
        IF @NestedTran = 0 commit tran;
        RETURN;
    END

    INSERT INTO InterviewAIQuestionSet (InterviewId, SetId, AssignedOn)
    VALUES (@InterviewId, @SetId, GETDATE());

    SET @Message = 'Assigned.';

  IF @NestedTran = 0 commit tran;
 end try
 begin catch
  IF @NestedTran = 1
  BEGIN
   IF XACT_STATE() = 1 ROLLBACK TRANSACTION AssignAIQSet;
  END
  ELSE IF @@TRANCOUNT > 0
   rollback transaction

  DECLARE @ErrMessage varchar(MAX) = ERROR_MESSAGE(),
   @Severity int = ERROR_SEVERITY(),
   @State smallint = ERROR_STATE()

  RAISERROR (@ErrMessage, @Severity, @State)
 end catch
END
GO
