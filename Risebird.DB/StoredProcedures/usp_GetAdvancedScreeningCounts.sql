IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAdvancedScreeningCounts'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAdvancedScreeningCounts
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: counts for the Screen All dialog and the candidate-list header.
-- Only candidates with a resume are counted. "Unscreened" matches usp_QueueAdvancedScreening's
-- Unscreened scope (no row, Failed or Skipped); "Stale" is completed on an older rubric or
-- older instructions.
Create PROCEDURE [dbo].[usp_GetAdvancedScreeningCounts]
@IRFId int
AS
Begin
	SET NOCOUNT ON;

	declare @rubricVersion int, @instructionsVersion int
	select @rubricVersion = RubricVersion, @instructionsVersion = InstructionsVersion
	from IRFScreeningRubric where IRFId = @IRFId

	select
		count(*) as TotalWithResume,
		sum(case when cas.id is null or cas.[Status] in ('Failed', 'Skipped') then 1 else 0 end) as Unscreened,
		sum(case when cas.[Status] = 'Completed'
		          and (ISNULL(cas.RubricVersion, 0) < ISNULL(@rubricVersion, 0)
		               or ISNULL(cas.InstructionsVersion, 0) < ISNULL(@instructionsVersion, 0)) then 1 else 0 end) as Stale,
		sum(case when cas.[Status] = 'Completed'
		          and (ISNULL(cas.InstructionsVersion, 0) < ISNULL(@instructionsVersion, 0)) then 1 else 0 end) as StaleInstructions,
		sum(case when cas.[Status] = 'Completed'
		          and (ISNULL(cas.RubricVersion, 0) < ISNULL(@rubricVersion, 0)) then 1 else 0 end) as StaleRubric,
		sum(case when cas.[Status] = 'Completed' then 1 else 0 end) as Screened,
		sum(case when cas.[Status] = 'Pending' then 1 else 0 end) as Pending,
		sum(case when cas.[Status] = 'Processing' then 1 else 0 end) as Processing,
		sum(case when cas.[Status] = 'Failed' then 1 else 0 end) as Failed,
		sum(case when cas.[Status] = 'Skipped' then 1 else 0 end) as Skipped,
		ISNULL(@rubricVersion, 0) as RubricVersion,
		ISNULL(@instructionsVersion, 0) as InstructionsVersion
	from Candidate ca
	left join CandidateAdvancedScreening cas on cas.CandidateId = ca.id
	where ca.IRFId = @IRFId and ISNULL(ca.ResumeLink, '') <> ''
End
GO
