IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchSchedulerDetailsByUniqueId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchSchedulerDetailsByUniqueId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure usp_FetchSchedulerDetailsByUniqueId    
@uniqueid varchar(50)    
AS    
BEGIN    
Select sie.*,IRF.MeetingPlatform,'IRF' schedulertype,IRF.SkillSetsevaluate,  
case when IRF.IsNewSkillChannel=1 then case When IRF.IRFId='TRANAIML90' then 90 else Isnull(pm.DurationSelectedMin,0) end else 60 end as Interviewduration from ScheduleInterviewEvent sie join IRF on IRF.IRFId=sie.SkillChannel     
left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType    
where uniqueid=@uniqueid and  IsActive=1 and IsNull(IsSingleUse,0)=0     
union     
Select sie.*,IRF.MeetingPlatform,'IRF' schedulertype,IRF.SkillSetsevaluate,  
case when IRF.IsNewSkillChannel=1 then case When IRF.IRFId='TRANAIML90' then 90 else Isnull(pm.DurationSelectedMin,0) end else 60 end as Interviewduration from ScheduleInterviewEvent sie join IRF on IRF.IRFId=sie.SkillChannelRelation     
left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType    
where uniqueid=@uniqueid and IsActive=1 and IsNull(IsOnetimeUsed,0)=0 and IsSingleUse=1     
union     
Select sie.*,'Zoho' MeetingPlatform,'Interviewer' schedulertype,1 as SkillSetsevaluate,60 as Interviewduration from ScheduleInterviewEvent sie join InterviewerSkillsChannel IRF on IRF.InterviewerSkillId=sie.SkillChannel     
where uniqueid=@uniqueid and  IsActive=1 and IsNull(IsSingleUse,0)=0     
union     
Select sie.*,'Zoho' MeetingPlatform,'Interviewer' schedulertype,1 as SkillSetsevaluate,60 as Interviewduration from ScheduleInterviewEvent sie join InterviewerSkillsChannel IRF on IRF.InterviewerSkillId=sie.SkillChannelRelation    
where uniqueid=@uniqueid and IsActive=1 and IsNull(IsOnetimeUsed,0)=0 and IsSingleUse=1    
END 


GO