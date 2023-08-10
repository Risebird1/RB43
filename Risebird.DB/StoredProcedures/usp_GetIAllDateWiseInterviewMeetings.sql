/****** Object:  StoredProcedure [dbo].[usp_GetIAllDateWiseInterviewMeetings]    Script Date: 05-11-2019 6.03.39 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIAllDateWiseInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIAllDateWiseInterviewMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetIAllDateWiseInterviewMeetings]                            
@startDate datetime,                        
@endDate datetime,                
@EmailId varchar(100)=null                
AS                 
                
--declare @idProfile int                
--set @idProfile=(select)                
Begin                                  
                      
if(@EmailId is not null and ((select idProfile from UserList where EmailId=@EmailId)=2))                      
 Begin                    
                 
                
 select distinct im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration, ulc.EmailId as candidateEmail,                     
  im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, IRF.JobCountry,                               
  im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime,                   
  case when ulc.LastName is null then ulc.FirstName                  
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                  
  end as candidateName,                  
  im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,                                
  IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,                          
  ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId                               
  ,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline,                      
  im.HRCoordinatorEmailId, im.HRComment, cl.[Name] as Client,                      
  qcfr.QCStatus,ulcu.EmailId as BillingOwner, ic.VideoDuration ,IRF.RisebirdHr as ClientHR,            
  ca.ResumeLink as resumeLink, ca.Phone as MobileNumber, ca.ClientHRAttemptContact as CandidateClientHR, irf.IsQCRequired,im.IsCandidateResponsed,im.CandidateResponse,im.ProctorReport,im.Cancellationsource            
  , itp.ResumeLink as interviewerresumeLink, itp.CodedResumeLink as encodedresumeLink,ca.IsProctor, im.ZoomMeetingId      
  , im.HasDistinctParticipants, irf.CreatedFromTemplateId, csc.[Name] as TemplateName,IRF.IsAadhaarVerificationRequired,    
  ulc.AadhaarVerified,im.RedoInterview,ISNULL(IRF.MeetingPlatform,'Zoom') as MeetingPlatform,    
  im.BackUpLink, im.BackUpLinkId,  ulcho.EmailId as ChannelOwner,    
  Case When IRF.IsNewSkillChannel=1 and ti.Surcharge=0 then 1 else 0 end as IsSurcharge,itp.isVerified,itp.IsEmployed,itp.CompanyName,itp.EmploymentComment      
  from  InterviewMeetings im                                
  inner join IRF on im.IRFId = IRF.IRFId                                
  left join UserList ulc on im.candidateEmail= ulc.EmailId                              
  left join UserList ulh on im.ClientHrContact= ulh.EmailId                              
  left join UserList uli on im.interviewer= uli.EmailId       
  left join TransInterviewer ti on ti.Userid=uli.id and ti.IRFId=IRF.id      
  left join Userlist ulcu on IRF.BillingOwner  =ulcu.id       
  left join UserList ulcho on ulcho.id  = irf.RisebirdHr    
  left join InterviewerProfile itp on itp.UserId = uli.id        
  left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId                                
  join CandidatesMaster cm on ulc.id = cm.UserId                          
  left join QCFeedbackReport qcfr on qcfr.InterviewId=im.id                          
  left join Client cl on cl.id = irf.ClientId                                 
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId                          
  join TransHR th on th.IRFId = irf.id                      
  join HR on HR.id = th.HRId                      
  join UserList ulhr on ulhr.id = HR.UserId      
  left join ClonableSkillChannel csc on csc.id = irf.CreatedFromTemplateId      
  left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType    
  where interviewDate >= @startDate AND interviewDate < @endDate and ulhr.EmailId=@EmailId                      
  Union                    
  select distinct im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration, ulc.EmailId as candidateEmail,                     
                    
  im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, IRF.JobCountry,                               
  im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime,                   
  case                   
  when ulc.LastName is null then ulc.FirstName                  
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                  
  end as candidateName,                  
  im.currentEmployerName,                                 
  im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,                                
  IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,                          
  ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId                               
  ,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline,                      
  im.HRCoordinatorEmailId, im.HRComment, cl.[Name] as Client,                  
  qcfr.QCStatus,ulcu.EmailId as BillingOwner, ic.VideoDuration,IRF.RisebirdHr as ClientHR,            
  ca.ResumeLink as resumeLink, ca.Phone as MobileNumber, ca.ClientHRAttemptContact as CandidateClientHR, irf.IsQCRequired,im.IsCandidateResponsed,im.CandidateResponse,im.ProctorReport,im.Cancellationsource                         
  , itp.ResumeLink as interviewerresumeLink, itp.CodedResumeLink as encodedresumeLink,ca.IsProctor, im.ZoomMeetingId      
  , im.HasDistinctParticipants, irf.CreatedFromTemplateId, csc.[Name] as TemplateName,IRF.IsAadhaarVerificationRequired,    
  ulc.AadhaarVerified,im.RedoInterview,ISNULL(IRF.MeetingPlatform,'Zoom') as MeetingPlatform,    
  im.BackUpLink,im.BackUpLinkId,  ulr.EmailId as ChannelOwner,    
  Case When IRF.IsNewSkillChannel=1 and ti.Surcharge=0 then 1 else 0 end as IsSurcharge ,itp.isVerified,itp.IsEmployed,itp.CompanyName,itp.EmploymentComment     
  from InterviewMeetings im                                
  inner join IRF on im.IRFId = IRF.IRFId                                
  left join UserList ulc on im.candidateEmail= ulc.EmailId                              
  left join UserList ulh on im.ClientHrContact= ulh.EmailId                              
  left join UserList uli on im.interviewer= uli.EmailId          
  left join TransInterviewer ti on ti.Userid=uli.id and ti.IRFId=IRF.id      
  left join Userlist ulcu on IRF.BillingOwner  =ulcu.id           
  left join InterviewerProfile itp on itp.UserId = uli.id        
  left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId                                
  join CandidatesMaster cm on ulc.id = cm.UserId                          
  left join QCFeedbackReport qcfr on qcfr.InterviewId=im.id                          
  left join Client cl on cl.id = irf.ClientId                               
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId                          
  left join UserList ulr on ulr.id = irf.RisebirdHr      
  left join ClonableSkillChannel csc on csc.id = irf.CreatedFromTemplateId      
  left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType    
  where interviewDate >= @startDate AND interviewDate < @endDate and ulr.EmailId=@EmailId                 
                      
 End                      
                      
else if(@EmailId is not null and ((select idProfile from UserList where EmailId=@EmailId)=31))                             
 Begin                      
  select distinct im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration, ulc.EmailId as candidateEmail,                     
  im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, IRF.JobCountry,                               
  im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime,                   
  case                   
  when ulc.LastName is null then ulc.FirstName                  
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                  
  end as candidateName,                  
  im.currentEmployerName,                                 
  im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,                                
  IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,                          
  ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId                               
  ,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline,                      
  im.HRCoordinatorEmailId, im.HRComment, cl.[Name] as Client,                      
  qcfr.QCStatus,ulcu.EmailId as BillingOwner, ic.VideoDuration,  ca.ResumeLink as resumeLink,            
  ca.Phone as MobileNumber, ca.ClientHRAttemptContact as CandidateClientHR, irf.IsQCRequired,im.IsCandidateResponsed ,im.CandidateResponse,im.ProctorReport,im.Cancellationsource                         
  , itp.ResumeLink as interviewerresumeLink, itp.CodedResumeLink as encodedresumeLink,ca.IsProctor, im.ZoomMeetingId      
  , im.HasDistinctParticipants, irf.CreatedFromTemplateId, csc.[Name] as TemplateName,IRF.IsAadhaarVerificationRequired,    
  ulc.AadhaarVerified,im.RedoInterview,ISNULL(IRF.MeetingPlatform,'Zoom') as MeetingPlatform,    
  im.BackUpLink,im.BackUpLinkId,  ulcho.EmailId as ChannelOwner,    
  Case When IRF.IsNewSkillChannel=1 and ti.Surcharge=0 then 1 else 0 end as IsSurcharge ,itp.isVerified,itp.IsEmployed,itp.CompanyName,itp.EmploymentComment     
  from InterviewMeetings im                                
  inner join IRF on im.IRFId = IRF.IRFId              
  join SeniorHR sh on sh.Clientid=IRF.ClientId            
  left join UserList ulc on im.candidateEmail= ulc.EmailId                              
  left join UserList ulh on im.ClientHrContact= ulh.EmailId                              
  left join UserList uli on im.interviewer= uli.EmailId        
  left join TransInterviewer ti on ti.Userid=uli.id and ti.IRFId=IRF.id      
  left join Userlist ulcu on IRF.BillingOwner  =ulcu.id      
  left join UserList ulcho on ulcho.id  = irf.RisebirdHr    
  left join InterviewerProfile itp on itp.UserId = uli.id        
  left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId                                
  join CandidatesMaster cm on ulc.id = cm.UserId                          
  left join QCFeedbackReport qcfr on qcfr.InterviewId=im.id                          
  left join Client cl on cl.id = irf.ClientId                               
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId                          
  left join Userlist ulsh on ulsh.id=sh.userid      
  left join ClonableSkillChannel csc on csc.id = irf.CreatedFromTemplateId      
  left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType    
  where interviewDate >= @startDate AND interviewDate < @endDate and ulsh.EmailId =@EmailId                     
 End                      
else                      
 Begin                      
  select distinct im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration, ulc.EmailId as candidateEmail,                     
  im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, IRF.JobCountry,                               
  im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime,                   
  case                   
  when ulc.LastName is null then ulc.FirstName                  
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                  
  end as candidateName,                  
  im.currentEmployerName,                                 
  im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,                                
  IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,                          
  ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId                               
  ,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline,                      
  im.HRCoordinatorEmailId, im.HRComment, cl.[Name] as Client,                      
  qcfr.QCStatus,ulcu.EmailId as BillingOwner, ic.VideoDuration,  ca.ResumeLink as resumeLink,            
  ca.Phone as MobileNumber, ca.ClientHRAttemptContact as CandidateClientHR, irf.IsQCRequired,im.IsCandidateResponsed  ,im.CandidateResponse,im.ProctorReport,im.Cancellationsource                        
  , itp.ResumeLink as interviewerresumeLink, itp.CodedResumeLink as encodedresumeLink,ca.IsProctor, im.ZoomMeetingId       
  , im.HasDistinctParticipants, irf.CreatedFromTemplateId, csc.[Name] as TemplateName,IRF.IsAadhaarVerificationRequired,    
  ulc.AadhaarVerified,im.RedoInterview,ISNULL(IRF.MeetingPlatform,'Zoom') as MeetingPlatform,    
  im.BackUpLink,im.BackUpLinkId,  ulcho.EmailId as ChannelOwner,    
  Case When IRF.IsNewSkillChannel=1 and ti.Surcharge=0 then 1 else 0 end as IsSurcharge,itp.isVerified,itp.IsEmployed,itp.CompanyName,itp.EmploymentComment      
  from InterviewMeetings im                                
  inner join IRF on im.IRFId = IRF.IRFId                                
  left join UserList ulc on im.candidateEmail= ulc.EmailId                              
  left join UserList ulh on im.ClientHrContact= ulh.EmailId                              
  left join UserList uli on im.interviewer= uli.EmailId        
  left join TransInterviewer ti on ti.Userid=uli.id and ti.IRFId=IRF.id      
  left join Userlist ulcu on IRF.BillingOwner  =ulcu.id     
  left join UserList ulcho on ulcho.id  = irf.RisebirdHr    
   left join InterviewerProfile itp on itp.UserId = uli.id        
  left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId                                
  join CandidatesMaster cm on ulc.id = cm.UserId                          
  left join QCFeedbackReport qcfr on qcfr.InterviewId=im.id                          
  left join Client cl on cl.id = irf.ClientId                               
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId                          
  left join ClonableSkillChannel csc on csc.id = irf.CreatedFromTemplateId       
  left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType    
  where interviewDate >= @startDate AND interviewDate < @endDate                       
 End                                
                                 
End                     
        
                       
                 
GO

