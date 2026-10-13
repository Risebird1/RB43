IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFScreeningRubric'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFScreeningRubric
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: an IRF's screening rubric together with the IRF inputs it is built
-- from. Always returns a row for an existing IRF; HasRubric = 0 means no rubric row exists yet.
Create PROCEDURE [dbo].[usp_GetIRFScreeningRubric]
@IRFId int
AS
Begin
	SET NOCOUNT ON;

	select irf.id as IRFId, irf.IRFId as IRFName, irf.JDTiltle, irf.JDLink, irf.IdealCandidateProfile,
	ISNULL(irf.IsAdvancedResumeScreening, 0) as IsAdvancedResumeScreening, irf.ClientId,
	case when r.id is null then 0 else 1 end as HasRubric,
	r.id as RubricId, r.JDText, r.Instructions, ISNULL(r.InstructionsVersion, 0) as InstructionsVersion,
	r.InstructionsUpdatedBy, r.InstructionsUpdatedOn,
	ISNULL(r.ApplyInstructionsToRubric, 1) as ApplyInstructionsToRubric,
	ISNULL(r.ApplyInstructionsToScoring, 1) as ApplyInstructionsToScoring,
	r.RubricJson, ISNULL(r.RubricVersion, 0) as RubricVersion, ISNULL(r.[Status], 'Draft') as [Status],
	r.TotalWeight, r.LastError, r.GeneratedOn, r.ApprovedBy, r.ApprovedOn,
	r.CreatedBy, r.CreatedOn, r.ModifiedBy, r.ModifiedOn
	from IRF irf
	left join IRFScreeningRubric r on r.IRFId = irf.id
	where irf.id = @IRFId
End
GO
