IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchJDTitleByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchJDTitleByIRFId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_FetchJDTitleByIRFId
@IRFId varchar(100)
AS
BEGIN
Select Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle, JDLink,pm.DurationSelectedMin as InterviewDuration  from IRF
left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType
where IRFId=@IRFId 
union 
Select JDTiltle, JDLink,55 as InterviewDuration from InterviewerSkillsChannel 
where InterviewerSkillId=@IRFId
END
GO