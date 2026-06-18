IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewersWithOutBillingRate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewersWithOutBillingRate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetInterviewersWithOutBillingRate]    
@Clientid int = NULL
AS    
Begin        
  select ti.IRFId, 'Interviewer' as [Type] ,ul.EmailId, ipr.ResumeLink, ipr.UserId, irf.IRFId as IRFName, ti.AIVerdict,       
  dbo.fn_GetInterviewerOrQCMeetingsCount(IRF.IRFId, ul.EmailId, 'I') as TotalInterviews, agc.CampaignName,       
  0 as TotalQCInterviews,ul.MobileNumber,ul.uniqueid,(ul.FirstName+' '+ul.LastName) as InterviewerName,  
  Case when ti.IsActive=1 and ul.isactive=1 then 1 else 0 end as IsActive ,ti.IsAvailable,  
  IRF.SendIntervieweravailbilityOn,IsNull(DATEDIFF(SECOND, IRF.SendIntervieweravailbilityOn, GetDate()),0) as AvailbilityHour,  
  ipr.isVerified,ipr.IsEmployed,ipr.CompanyName,ipr.EmploymentComment,ipr.AvailabilityResponseDate,ti.PerSuccessRate,IRF.JDLink,  
  Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTitle,  
  IRF.InterviewDuration, ipr.VideoConfirmation,ipr.RedMarker, ti.[Source], ti.ClientAction,ibr.BillingRate,Client.[Name] as ClientName,IRF.JobCountry,IRF.CompanyType,IRF.Heuristicsscore      
  from TransInterviewer ti join InterviewerProfile ipr on ipr.UserId = ti.UserId        
  join UserList ul on ul.id = ti.UserId        
  join IRF on irf.id = ti.IRFId    
  join Client on IRF.ClientId = Client.id  
  left join AGCampaigns agc on agc.id = ti.CampaignId  
  left join InterviewerBillingRate ibr on ibr.IRFId=ti.IRFId and ibr.Userid=ti.UserId  
  where ISNULL(Client.InterviewerBillingType,1) = 3
    and (@Clientid IS NULL OR IRF.Clientid = @Clientid)
    and Isnull(ibr.BillingRate,0)=0
  group by ti.IRFId, ul.EmailId, ipr.ResumeLink, ipr.UserId, irf.IRFId, ti.AIVerdict, ipr.ResumeLink, ul.MobileNumber, IRF.IRFId,ul.uniqueid,ul.FirstName+' '+ul.LastName,Case when ti.IsActive=1 and ul.isactive=1 then 1 else 0 end,ti.IsAvailable,  
  IRF.SendIntervieweravailbilityOn,ipr.isVerified,ipr.IsEmployed,ipr.CompanyName,ipr.EmploymentComment,ipr.AvailabilityResponseDate,  
  ti.PerSuccessRate,IRF.JDLink, Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,IRF.InterviewDuration,ipr.VideoConfirmation,ipr.RedMarker, ti.[Source], ti.ClientAction, agc.CampaignName,ibr.BillingRate,Client.[Name],IRF.JobCountry,IRF.CompanyType,IRF.Heuristicsscore         
End   
GO