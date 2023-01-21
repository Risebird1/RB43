  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchAllInterviewerupcommingInterviewes'))
BEGIN
    DROP PROCEDURE [dbo].[usp_FetchAllInterviewerupcommingInterviewes]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
CREATE Procedure usp_FetchAllInterviewerupcommingInterviewes    
@timeNow datetime,  
@type varchar(10)  
AS      
BEGIN      
Declare @Count int=0  
if(@type='Verified')  
BEGIN  
select @Count= Count(distinct ul.EmailId) from TransInterviewer ti join UserList ul on ul.id = ti.UserId   
join IRF on IRF.id=ti.IRFId  
join InterviewMeetings im on im.IRFId=IRF.IRFId  
join InterviewerAssessmentVerifications iav on iav.UserId=ul.id and IRF.AssessmentGroupId=iav.AssessmentId  
where ti.IsActive=1 and interviewer is null and confirmationStatus = 1 and IsEmergency=0 and im.interviewDate>=@timeNow and iav.Isverified=1 and ul.IsActive=1 
END  
  
if(@Count=0)  
BEGIN  
;With CTE as (                
select im.id, im.IRFId, im.interviewDate,im.candidateEmail,           
im.interviewer,ul.EmailId from InterviewMeetings im              
inner join IRF irf on im.IRFId=irf.IRFId               
inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId              
left join [dbo].[InterviewerProfile] inte on tirf.InterviewerId = inte.id          
left join [dbo].[UserList] ul on inte.UserId = ul.id           
where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and im.interviewDate>=@timeNow and tirf.IsActive=1 and ul.IsActive=1)       
Select CTE.EmailId,COUNT(CTE.id) as Total,ip.uniqueid from CTE left join InterviewerPublicURLs ip on ip.Emailid=CTE.EmailId group by CTE.EmailId,ip.uniqueid      
END  
ELSE  
BEGIN  
;With CTE as (                
select im.id, im.IRFId, im.interviewDate,im.candidateEmail,           
im.interviewer,ul.EmailId from InterviewMeetings im              
inner join IRF irf on im.IRFId=irf.IRFId               
inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId              
left join [dbo].[InterviewerProfile] inte on tirf.InterviewerId = inte.id          
join InterviewerAssessmentVerifications iav on iav.UserId=inte.UserId and iav.AssessmentId=IRF.AssessmentGroupId  
left join [dbo].[UserList] ul on inte.UserId = ul.id           
where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and im.interviewDate>=@timeNow and tirf.IsActive=1 and iav.Isverified=1 and ul.IsActive=1)       
Select CTE.EmailId,COUNT(CTE.id) as Total,ip.uniqueid from CTE left join InterviewerPublicURLs ip on ip.Emailid=CTE.EmailId group by CTE.EmailId,ip.uniqueid      
END  
  
END 
GO  
    

  