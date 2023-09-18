IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_GetAssessmentGroups'))
BEGIN
    DROP PROCEDURE dbo.sp_GetAssessmentGroups
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[sp_GetAssessmentGroups]     
   
AS    
BEGIN    

 CREATE TABLE #temp_AssessmentInterviewerCount
      (
      AssessmentGroupId int,
	  InterviewerCount int
      )
INSERT INTO #temp_AssessmentInterviewerCount
      (
      AssessmentGroupId,InterviewerCount
      )
Select ag.id, count(ipr.id) from InterviewerProfile ipr 
join UserList ul on ul.id = ipr.UserId
cross Apply string_split(AssessmentGroupId,',') as f
join AssessmentGroup as ag on f.value =ag.id 
where ul.IsActive = 1
group by  ag.id

	   CREATE TABLE #temp_AssessmentIRFCount
      (
      AssessmentGroupId int,
	  IRFCount int
      )
INSERT INTO #temp_AssessmentIRFCount
      (
      AssessmentGroupId,IRFCount
      )
select ag.id,Count(IRF.id)
from AssessmentGroup ag 
left join AssessmentGroupCategory agc on agc.id=ag.Categoryid 
left join IRF on IRF.AssessmentGroupId=ag.id
group by ag.id

select ag.id,AssessmentGroup,PrimarySkills,
ag.Isinstaavailable,agc.CategoryName,ag.CompanyType,ag.IsRelevant,ag.ProfileType,ag.CompetencyLevel,ag.Country,ag.PerHourRate,
 afip.InterviewerCount as InterviewerCount, agrf.IRFCount as IrfCount 
from AssessmentGroup ag 
join #temp_AssessmentIRFCount as agrf on ag.id=agrf.AssessmentGroupId
left join #temp_AssessmentInterviewerCount as afip on ag.id= afip.AssessmentGroupId
left join AssessmentGroupCategory agc on agc.id=ag.Categoryid 
group by ag.id,AssessmentGroup,PrimarySkills,
ag.Isinstaavailable,agc.CategoryName,ag.CompanyType,ag.IsRelevant,ag.ProfileType,ag.CompetencyLevel,ag.Country,ag.PerHourRate,afip.InterviewerCount,agrf.IRFCount

 DROP TABLE #temp_AssessmentIRFCount

 DROP TABLE #temp_AssessmentInterviewerCount
    
END
Go