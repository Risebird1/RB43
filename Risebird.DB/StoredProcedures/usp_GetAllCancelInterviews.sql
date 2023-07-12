/****** Object:  StoredProcedure [dbo].[usp_GetIAllDateWiseInterviewMeetings]    Script Date: 05-11-2019 6.03.39 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAllCancelInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllCancelInterviews
END

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetAllCancelInterviews]                        
@startDate datetime,                    
@endDate datetime,            
@EmailId varchar(100)=null            
AS             
            
--declare @idProfile int            
--set @idProfile=(select)            
Begin                              
                  
if(@EmailId is not null and ((select idProfile from UserList where EmailId=@EmailId)=2))                  
 Begin                
             
            
 select distinct 
 im.id imid,
 im.IRFId, 
 im.interviewDate, 
  ulc.EmailId as candidateEmail,                 
  im.ClientHrContact,                                          
  case when ulc.LastName is null then ulc.FirstName  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))  end as candidateName,                                
  cl.[Name] as Client,                  
  ulcu.EmailId as BillingOwner,
  im.Cancellationsource
  from  InterviewMeetings im                            
   inner join IRF on im.IRFId = IRF.IRFId                            
  left join UserList ulc on im.candidateEmail= ulc.EmailId                          
  left join UserList ulh on im.ClientHrContact= ulh.EmailId                          
  left join UserList uli on im.interviewer= uli.EmailId   
  --left join TransInterviewer ti on ti.Userid=uli.id and ti.IRFId=IRF.id  --??
  left join Userlist ulcu on IRF.BillingOwner  =ulcu.id       
 -- left join InterviewerProfile itp on itp.UserId = uli.id --??   
  --left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId                            
  join CandidatesMaster cm on ulc.id = cm.UserId                      
--  left join QCFeedbackReport qcfr on qcfr.InterviewId=im.id  --?                    
  left join Client cl on cl.id = irf.ClientId                             
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId                      
 -- join TransHR th on th.IRFId = irf.id   --??               
 --- join HR on HR.id = th.HRId                  
--  join UserList ulhr on ulhr.id = HR.UserId  
  --left join ClonableSkillChannel csc on csc.id = irf.CreatedFromTemplateId  --?
  --left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType --?
  where interviewDate >= @startDate AND interviewDate < @endDate and uli.EmailId=@EmailId AND im.confirmationStatus=2            
  Union                
  select distinct im.id imid,
  im.IRFId, 
  im.interviewDate,
  ulc.EmailId as candidateEmail, 
  im.ClientHrContact,
  case               
  when ulc.LastName is null then ulc.FirstName              
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))              
  end as candidateName,              
  cl.[Name] as Client,              
  ulcu.EmailId as BillingOwner,
  im.Cancellationsource 
  from 
  InterviewMeetings im                            
  inner join IRF on im.IRFId = IRF.IRFId                            
  left join UserList ulc on im.candidateEmail= ulc.EmailId                          
 -- left join UserList ulh on im.ClientHrContact= ulh.EmailId --?                         
 -- left join UserList uli on im.interviewer= uli.EmailId  --?    
 -- left join TransInterviewer ti on ti.Userid=uli.id and ti.IRFId=IRF.id  --?
  left join Userlist ulcu on IRF.BillingOwner  =ulcu.id       
 -- left join InterviewerProfile itp on itp.UserId = uli.id --?   
 -- left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId --?                           
--  join CandidatesMaster cm on ulc.id = cm.UserId --?                      
--  left join QCFeedbackReport qcfr on qcfr.InterviewId=im.id    --?                  
  left join Client cl on cl.id = irf.ClientId                           
--  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId  --?                    
  left join UserList ulr on ulr.id = irf.RisebirdHr  
--  left join ClonableSkillChannel csc on csc.id = irf.CreatedFromTemplateId  --?
--  left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType--?
  where interviewDate >= @startDate AND interviewDate < @endDate and ulr.EmailId=@EmailId AND im.confirmationStatus=2        
                  
 End                  
                  
else if(@EmailId is not null and ((select idProfile from UserList where EmailId=@EmailId)=31))                         
 Begin                  
  select distinct im.id imid,
  im.IRFId, 
  im.interviewDate, 
  ulc.EmailId as candidateEmail,                 
  im.ClientHrContact,               
  case               
  when ulc.LastName is null then ulc.FirstName              
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))              
  end as candidateName,              
  cl.[Name] as Client,                  
  ulcu.EmailId as BillingOwner ,
  im.Cancellationsource
  from InterviewMeetings im                            
  inner join IRF on im.IRFId = IRF.IRFId          
  join SeniorHR sh on sh.Clientid=IRF.ClientId        
  left join UserList ulc on im.candidateEmail= ulc.EmailId                          
  --left join UserList ulh on im.ClientHrContact= ulh.EmailId                          
  --left join UserList uli on im.interviewer= uli.EmailId    
  --left join TransInterviewer ti on ti.Userid=uli.id and ti.IRFId=IRF.id  
  left join Userlist ulcu on IRF.BillingOwner  =ulcu.id          
  --left join InterviewerProfile itp on itp.UserId = uli.id    
  --left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId                            
  --join CandidatesMaster cm on ulc.id = cm.UserId                      
  --left join QCFeedbackReport qcfr on qcfr.InterviewId=im.id                      
  left join Client cl on cl.id = irf.ClientId                           
  --join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId                      
  left join Userlist ulsh on ulsh.id=sh.userid  
  --left join ClonableSkillChannel csc on csc.id = irf.CreatedFromTemplateId  
  --left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType
  where interviewDate >= @startDate AND interviewDate < @endDate and ulsh.EmailId =@EmailId  AND im.confirmationStatus=2               
 End                  
else                  
 Begin                  
select distinct
 im.id imid,
 im.IRFId, 
 im.interviewDate, 
 ulc.EmailId as candidateEmail,                 
  im.ClientHrContact,                               
  case when ulc.LastName is null then ulc.FirstName else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName)) end as candidateName,              
  cl.[Name] as Client,                  
  ulcu.EmailId as BillingOwner,
 im.Cancellationsource 
  from InterviewMeetings im                            
  inner join IRF on im.IRFId = IRF.IRFId                            
  left join UserList ulc on im.candidateEmail= ulc.EmailId                          
  
  left join Userlist ulcu on IRF.BillingOwner  =ulcu.id                            
  left join Client cl on cl.id = irf.ClientId   
  where interviewDate >= @startDate AND interviewDate < @endDate  AND im.confirmationStatus=2              
 End                            
                             
End                  
GO

