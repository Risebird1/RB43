IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateAdvancedScreening'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateAdvancedScreening
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: one candidate's result plus what the report page and the scorer
-- need around it - name, email, resume link, IRF title and the IRF's current rubric versions.
-- Returns a row for any existing candidate; HasResult = 0 when it has never been screened.
Create PROCEDURE [dbo].[usp_GetCandidateAdvancedScreening]
@CandidateId int
AS
Begin
	SET NOCOUNT ON;

	select ca.id as CandidateId, ca.CandidateMasterId, ca.IRFId, ca.ResumeLink,
	ul.EmailId as CandidateEmail,
	case when ul.LastName is null then ul.FirstName
	     else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName)) end as CandidateName,
	irf.IRFId as IRFName, irf.JDTiltle, ISNULL(irf.IsAdvancedResumeScreening, 0) as IsAdvancedResumeScreening,
	r.RubricVersion as CurrentRubricVersion, r.InstructionsVersion as CurrentInstructionsVersion,
	r.[Status] as RubricStatus,
	case when cas.id is null then 0 else 1 end as HasResult,
	cas.[Status], cas.RubricVersion, cas.InstructionsVersion, cas.TotalScore, cas.OverallPercentage, cas.Verdict,
	cas.SkillsScore, cas.WorkScore, cas.EducationScore, cas.AuthenticityRating, cas.AuthenticityFlagCount,
	cas.ResponseJson, cas.FactsJson, ISNULL(cas.AttemptCount, 0) as AttemptCount, cas.ErrorMessage,
	cas.ScreenedOn, cas.TriggeredBy
	from Candidate ca
	join CandidatesMaster cm on cm.id = ca.CandidateMasterId
	join UserList ul on ul.id = cm.UserId
	join IRF irf on irf.id = ca.IRFId
	left join IRFScreeningRubric r on r.IRFId = irf.id
	left join CandidateAdvancedScreening cas on cas.CandidateId = ca.id
	where ca.id = @CandidateId
End
GO
