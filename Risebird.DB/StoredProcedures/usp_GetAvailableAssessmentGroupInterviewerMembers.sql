IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAvailableAssessmentGroupInterviewerMembers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAvailableAssessmentGroupInterviewerMembers
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetAvailableAssessmentGroupInterviewerMembers      
@assessmentGroupId int,
@timeNow datetime,
@irfId int = null
AS      
BEGIN   


if(@irfId is not null and @irfId != 0)
Begin
	
	Begin

	declare @flatRate int = (select FlatRate from IRF where id = @irfId)
	declare @basicSkill varchar(200) = (select PrimarySkillInvite from IRF where id = @irfId)
	declare @lastSentOn datetime = (select SendIntervieweravailbilityOn from IRF where id = @irfId)
	declare @isNewSkillChannel bit = (select IsNewSkillChannel from IRF where id = @irfId)
	--declare @companyType int = (select CompanyType from IRF where id = @irfId)
	--declare @planType int = (select Heuristicsscore from IRF where id = @irfId)
	--declare @countryType int = (select JobCountry from IRF where id = @irfId)

	--declare  @calculatedFlatRate int = (select Rate from PlanTypeChargesMaster where CompanyType = @companyType and PlanTypeEnumValue = @planType and CountryType = @countryType)

	if(@isNewSkillChannel = 1)
	Begin
		;With CTE AS(      
		Select UserId, value from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',') where ipr.Isavailable in (1,2,3) or ipr.Isavailable is null)--where ipr.Isavailable = 1, global availability check removed for a while   
		Select distinct ul.EmailId, ul.uniqueid, (ul.FirstName+' '+ul.LastName) as InterviewerName, IsNull(DATEDIFF(SECOND, @lastSentOn, @timeNow), 0) as AvailbilityHour, @basicSkill as BasicSkill,
		@flatRate  as FlatRate
		From CTE join Userlist ul on CTE.Userid=ul.id       
		where CTE.value = @assessmentGroupId and ul.IsActive=1 
	End
	Else
	Begin

		declare  @calculatedFlatRate int = (select min(PerSuccessRate) from TransInterviewer where IRFId = @irfId)


		;With CTE AS(      
		Select UserId, value from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',') 
		where ipr.Isavailable in (1,2,3) or ipr.Isavailable is null)--where ipr.Isavailable = 1, global availability check removed for a while   
		select a.EmailId, a.uniqueid, a.InterviewerName, a.AvailbilityHour, a.BasicSkill, min(FlatRate) as  FlatRate from 
		(Select distinct ul.EmailId, ul.uniqueid, (ul.FirstName+' '+ul.LastName) as InterviewerName, IsNull(DATEDIFF(SECOND, @lastSentOn, @timeNow), 0) as AvailbilityHour, @basicSkill as BasicSkill,
		CASE
			WHEN PerSuccessRate > 0 THEN PerSuccessRate
			ELSE @calculatedFlatRate
		END AS FlatRate
		From CTE 
		join Userlist ul on CTE.Userid=ul.id 
		left join TransInterviewer ti on ti.UserId = ul.id
		where CTE.value = @assessmentGroupId and ul.IsActive=1 and (ti.IRFId = @irfId or ti.IRFId is null)) a
		group by a.EmailId, a.uniqueid, a.InterviewerName, a.AvailbilityHour, a.BasicSkill
	End

	End

	 
End
Else
Begin
	;With CTE AS(      
	Select UserId, value from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',') where ipr.Isavailable in (1,2,3) or ipr.Isavailable is null)--where ipr.Isavailable = 1, global availability check removed for a while     
	Select distinct ul.EmailId, ul.uniqueid, (ul.FirstName+' '+ul.LastName) as InterviewerName        
	From CTE join Userlist ul on CTE.Userid=ul.id       
	where CTE.value = @assessmentGroupId and ul.IsActive=1  
End

  
END
GO
 