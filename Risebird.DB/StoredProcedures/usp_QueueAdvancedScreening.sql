IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_QueueAdvancedScreening'))
BEGIN
    DROP PROCEDURE dbo.usp_QueueAdvancedScreening
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: queues candidates on an IRF for the job (Screen All / Re-screen).
--
-- @Scope
--   Unscreened           no completed result yet: no row, Failed or Skipped.
--   UnscreenedAndStale   the above, plus completed results scored on an older rubric or
--                        older instructions (the default in the Screen All dialog).
--   All                  everyone.
-- @CandidateId, when given, queues that one candidate whatever the scope.
--
-- Rows already Pending or Processing are left alone. Queued rows keep their last completed
-- scores until the new result is saved. Returns the number of candidates queued.
Create PROCEDURE [dbo].[usp_QueueAdvancedScreening]
@IRFId int,
@Scope varchar(30) = 'UnscreenedAndStale',
@CandidateId int = null,
@TriggeredBy varchar(200) = null
AS
Begin
	SET NOCOUNT ON;

	declare @rubricVersion int, @instructionsVersion int
	select @rubricVersion = RubricVersion, @instructionsVersion = InstructionsVersion
	from IRFScreeningRubric where IRFId = @IRFId

	declare @targets table (CandidateId int primary key)

	insert into @targets (CandidateId)
	select ca.id
	from Candidate ca
	left join CandidateAdvancedScreening cas on cas.CandidateId = ca.id
	where ca.IRFId = @IRFId
	and ISNULL(ca.ResumeLink, '') <> ''
	and (cas.id is null or cas.[Status] not in ('Pending', 'Processing'))
	and (
		(@CandidateId is not null and ca.id = @CandidateId)
		or (@CandidateId is null and (
			@Scope = 'All'
			or cas.id is null
			or cas.[Status] in ('Failed', 'Skipped')
			or (@Scope = 'UnscreenedAndStale' and cas.[Status] = 'Completed'
			    and (ISNULL(cas.RubricVersion, 0) < ISNULL(@rubricVersion, 0)
			         or ISNULL(cas.InstructionsVersion, 0) < ISNULL(@instructionsVersion, 0)))
		))
	)

	update cas set [Status] = 'Pending', AttemptCount = 0, ErrorMessage = null, ClaimedOn = null,
		QueuedOn = GETUTCDATE(), TriggeredBy = @TriggeredBy
	from CandidateAdvancedScreening cas
	join @targets t on t.CandidateId = cas.CandidateId

	insert into CandidateAdvancedScreening (CandidateId, IRFId, [Status], QueuedOn, TriggeredBy)
	select t.CandidateId, @IRFId, 'Pending', GETUTCDATE(), @TriggeredBy
	from @targets t
	where not exists (select 1 from CandidateAdvancedScreening cas where cas.CandidateId = t.CandidateId)

	select count(*) as QueuedCount from @targets
End
GO
