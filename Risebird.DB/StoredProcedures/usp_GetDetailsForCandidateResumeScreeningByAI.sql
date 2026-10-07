

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDetailsForCandidateResumeScreeningByAI'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDetailsForCandidateResumeScreeningByAI
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetDetailsForCandidateResumeScreeningByAI]  
@IRFId int,   
@CandidateId int
AS  
Begin
	select distinct irf.IRFId as IRFName, irf.JDTiltle, irf.JobLevel, irf.CompanyType, ca.ResumeLink,
	ca.AIResumeScreeningVerdict, ca.AIScreeningResultJsonV2, ag.AssessmentGroup,
	ca.AIResumeScreeningScore, ca.AIAdvancedScreeningResultJson
	from Candidate ca join CandidatesMaster cm on ca.CandidateMasterId = cm.id
	join UserList ul on ul.id = cm.UserId
	join IRF on irf.id = ca.IRFId
	-- Left join: advanced screening scores against the IRF's own rubric and does not need an
	-- assessment group, so an IRF without one must still return its candidate.
	left join AssessmentGroup ag on ag.id = irf.AssessmentGroupId
	where ca.IRFId = @IRFId and ca.CandidateMasterId = @CandidateId


End
GO


