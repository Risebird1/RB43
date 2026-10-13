IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidatesForAdvancedScreening'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidatesForAdvancedScreening
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: claims the next batch of work for AdvancedResumeScreeningJob.
--
-- Eligible: candidates with a resume on IRFs where IsAdvancedResumeScreening = 1 and the rubric
-- is Approved, that have no result row (new candidates are picked up automatically), a Pending
-- row, a Failed row with AttemptCount < 3, or a row stuck in Processing for over 30 minutes.
--
-- The claim is atomic - UPDATE TOP ... WITH (READPAST, UPDLOCK) ... OUTPUT - so two job
-- instances (or the job and the web host) never pick the same row.
Create PROCEDURE [dbo].[usp_GetCandidatesForAdvancedScreening]
@BatchSize int = 20
AS
Begin
	SET NOCOUNT ON;

	-- New candidates on enabled, approved IRFs get a Pending row first.
	insert into CandidateAdvancedScreening (CandidateId, IRFId, [Status], QueuedOn, TriggeredBy)
	select ca.id, ca.IRFId, 'Pending', GETUTCDATE(), 'Job'
	from Candidate ca
	join IRF irf on irf.id = ca.IRFId and irf.IsAdvancedResumeScreening = 1
	join IRFScreeningRubric r on r.IRFId = irf.id and r.[Status] = 'Approved'
	where ISNULL(ca.ResumeLink, '') <> ''
	and not exists (select 1 from CandidateAdvancedScreening cas where cas.CandidateId = ca.id)

	;with nextBatch as (
		select top (@BatchSize) cas.*
		from CandidateAdvancedScreening cas with (READPAST, UPDLOCK, ROWLOCK)
		join IRF irf on irf.id = cas.IRFId and irf.IsAdvancedResumeScreening = 1
		join IRFScreeningRubric r on r.IRFId = cas.IRFId and r.[Status] = 'Approved'
		where cas.[Status] = 'Pending'
		   or (cas.[Status] = 'Failed' and cas.AttemptCount < 3)
		   or (cas.[Status] = 'Processing' and cas.ClaimedOn < DATEADD(minute, -30, GETUTCDATE()))
		order by cas.QueuedOn, cas.id
	)
	update nextBatch set [Status] = 'Processing', ClaimedOn = GETUTCDATE()
	output inserted.CandidateId, inserted.IRFId, inserted.TriggeredBy, inserted.AttemptCount
End
GO
