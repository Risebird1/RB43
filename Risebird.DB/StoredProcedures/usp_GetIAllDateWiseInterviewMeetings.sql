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
                       
                      
 select distinct im.id imid,im.IRFId,im.interviewDate, im.interviewSlot, Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration, ulc.EmailId as candidateEmail,                           
  im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, IRF.JobCountry, IRF.CalendlyLink,                                    
  im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime,                         
  case when ulc.LastName is null then ulc.FirstName                        
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                        
  end as candidateName,                        
  im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,im.SessionId,IRF.IsAIInterview,                                      
  IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.CalendlyLink,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,                                
  ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId                                     
  ,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline,                            
  im.HRCoordinatorEmailId, im.HRComment, cl.[Name] as Client,                            
  qcfr.QCStatus,ulcu.EmailId as BillingOwner, ic.VideoDuration ,IRF.RisebirdHr as ClientHR,                  
  ca.ResumeLink as resumeLink, ca.Phone as MobileNumber, ca.ClientHRAttemptContact as CandidateClientHR, irf.IsQCRequired,im.IsCandidateResponsed,im.CandidateResponse,im.ProctorReport,im.Cancellationsource                  
  , itp.ResumeLink as interviewerresumeLink, itp.CodedResumeLink as encodedresumeLink,ca.IsProctor, im.ZoomMeetingId            
  , im.HasDistinctParticipants, irf.CreatedFromTemplateId, csc.[Name] as TemplateName,IRF.IsAadhaarVerificationRequired,          
  ulc.AadhaarVerified,im.RedoInterview,ISNULL(im.MeetingPlatform,'Zoom') as MeetingPlatform,          
  im.BackUpLink, im.BackUpLinkId,  ulcho.EmailId as ChannelOwner,IRF.IsVideoRequired, IRF.CalendlyLink,          
  Case When IRF.IsNewSkillChannel=1 and ti.Surcharge=0 then 1 else 0 end as IsSurcharge,itp.isVerified,itp.IsEmployed,itp.CompanyName,itp.EmploymentComment,itp.RedMarker            
  --,si.Uniqueid as SchedulerUniqueid,ul.EmailId as CandidateEmailId      
  ,iman.interviewDate RescheduleDate, ti.PerSuccessRate, ic.AudioTranscriptFileLink, ic.IsAITranscriptAnalysisAttempted, im.AssistantId, im.SessionId, irf.IsAIInterview
  , d.NonAIreasoning, d.Riskcategory
  from  InterviewMeetings im                                      
  inner join IRF on im.IRFId = IRF.IRFId                                      
  left join UserList ulc on im.candidateEmail= ulc.EmailId                                    
  left join UserList ulh on im.ClientHrContact= ulh.EmailId                                    
  left join UserList uli on im.interviewer= uli.EmailId             
  left join TransInterviewer ti on ti.Userid=uli.id and ti.IRFId=IRF.id            
  left join Userlist ulcu on IRF.BillingOwner  =ulcu.id             
  left join UserList ulcho on ulcho.id  = irf.RisebirdHr            left join InterviewerProfile itp on itp.UserId = uli.id              
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
  outer Apply(Select top(1)ima.interviewDate From InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where im.IRFId=ima.IRFId and im.candidateEmail=ima.candidateEmail and ima.confirmationStatus=1 order by ima.Interviewdate desc) iman      
  --left join UserList ul on ul.id = cm.UserId      
  --left join ScheduleInterviewEvent si on si.SkillChannel=IRF.IRFId      
  LEFT JOIN (SELECT InterviewId, STRING_AGG(Type, ',') AS NonAIreasoning, STRING_AGG(Riskcategory, ',') AS Riskcategory FROM (
    Select im.id as InterviewId,'deficitmorethan10' as Type,'High Severity' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType
    where (MeetingStatus=4) and DateDiff(Day,im.interviewDate,Getdate())<30
    and (ic.VideoDuration-pt.DurationSelectedMin)>(-pt.DurationSelectedMin) and (ic.VideoDuration-pt.DurationSelectedMin)<-10
    union
    Select im.id as InterviewId,'AIVerdictLessthan4' as Type,'High Severity' as Riskcategory
    From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join Client cl on cl.id=IRF.ClientId
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30
    and ic.AITranscriptAnalysisScore<4
    union
    Select im.id as InterviewId,'CandidateRating' as Type,'High Severity' as Riskcategory
    From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join Client cl on cl.id=IRF.ClientId
    join CandidateFeedbackDetails cfd on cfd.InterviewId=im.id
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30 and cfd.Rating<4
    union
    Select im.id as InterviewId,'Selectdeficit' as Type,'Under Review' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30
    and (pt.DurationSelectedMin*2/3>ic.VideoDuration) and (InterviewStatus=2 or InterviewStatus=4)
    union
    Select im.id as InterviewId, Case when imj.Joinedon is null and imc.PerformedOn is null then 'No Show'
    when imj.Joinedon is null then 'Interviewer No Show' when imc.PerformedOn is null then 'Candidate No Show' end as Type,'Fake Report' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    outer apply(Select max(Joinedon) Joinedon from InterviewMeetingsJoinTime where Interviewid=im.id) imj
    outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc
    where DateDiff(Day,im.interviewDate,Getdate())<30 and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)
    and (imj.Joinedon is null or imc.PerformedOn is null) and ic.MeetingStatus!=5 and ic.MeetingStatus!=1 and ic.MeetingStatus!=9 and ic.MeetingStatus!=2 and ic.MeetingStatus!=3
  ) x GROUP BY InterviewId) d ON d.InterviewId = im.id
  where im.interviewDate >= @startDate AND im.interviewDate < @endDate and ulhr.EmailId=@EmailId                            
  Union                          
  select distinct im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration, ulc.EmailId as candidateEmail,                           
                          
  im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, IRF.JobCountry, IRF.CalendlyLink,                                    
  im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime,                         
  case                         
  when ulc.LastName is null then ulc.FirstName                        
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                        
  end as candidateName,                        
  im.currentEmployerName,                                       
  im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore, im.SessionId,IRF.IsAIInterview,                                      
  IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.CalendlyLink,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,                                
  ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId                                     
  ,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline,                            
  im.HRCoordinatorEmailId, im.HRComment, cl.[Name] as Client,                        
  qcfr.QCStatus,ulcu.EmailId as BillingOwner, ic.VideoDuration,IRF.RisebirdHr as ClientHR,                  
  ca.ResumeLink as resumeLink, ca.Phone as MobileNumber, ca.ClientHRAttemptContact as CandidateClientHR, irf.IsQCRequired,im.IsCandidateResponsed,im.CandidateResponse,im.ProctorReport,im.Cancellationsource                               
  , itp.ResumeLink as interviewerresumeLink, itp.CodedResumeLink as encodedresumeLink,ca.IsProctor, im.ZoomMeetingId            
  , im.HasDistinctParticipants, irf.CreatedFromTemplateId, csc.[Name] as TemplateName,IRF.IsAadhaarVerificationRequired,          
  ulc.AadhaarVerified,im.RedoInterview,ISNULL(im.MeetingPlatform,'Zoom') as MeetingPlatform,          
  im.BackUpLink,im.BackUpLinkId,  ulr.EmailId as ChannelOwner,IRF.IsVideoRequired, IRF.CalendlyLink,          
  Case When IRF.IsNewSkillChannel=1 and ti.Surcharge=0 then 1 else 0 end as IsSurcharge ,itp.isVerified,itp.IsEmployed,itp.CompanyName,itp.EmploymentComment,itp.RedMarker            
  --,si.Uniqueid as SchedulerUniqueid,ul.EmailId as CandidateEmailId      
  ,iman.interviewDate RescheduleDate,ti.PerSuccessRate, ic.AudioTranscriptFileLink, ic.IsAITranscriptAnalysisAttempted, im.AssistantId, im.SessionId, irf.IsAIInterview
  , d.NonAIreasoning, d.Riskcategory
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
  outer Apply(Select top(1)ima.interviewDate From InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where im.IRFId=ima.IRFId and im.candidateEmail=ima.candidateEmail and ima.confirmationStatus=1 order by ima.Interviewdate desc) iman      
  --left join UserList ul on ul.id = cm.UserId      
  --left join ScheduleInterviewEvent si on si.SkillChannel=IRF.IRFId      
  LEFT JOIN (SELECT InterviewId, STRING_AGG(Type, ',') AS NonAIreasoning, STRING_AGG(Riskcategory, ',') AS Riskcategory FROM (
    Select im.id as InterviewId,'deficitmorethan10' as Type,'High Severity' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType
    where (MeetingStatus=4) and DateDiff(Day,im.interviewDate,Getdate())<30
    and (ic.VideoDuration-pt.DurationSelectedMin)>(-pt.DurationSelectedMin) and (ic.VideoDuration-pt.DurationSelectedMin)<-10
    union
    Select im.id as InterviewId,'AIVerdictLessthan4' as Type,'High Severity' as Riskcategory
    From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join Client cl on cl.id=IRF.ClientId
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30
    and ic.AITranscriptAnalysisScore<4
    union
    Select im.id as InterviewId,'CandidateRating' as Type,'High Severity' as Riskcategory
    From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join Client cl on cl.id=IRF.ClientId
    join CandidateFeedbackDetails cfd on cfd.InterviewId=im.id
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30 and cfd.Rating<4
    union
    Select im.id as InterviewId,'Selectdeficit' as Type,'Under Review' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30
    and (pt.DurationSelectedMin*2/3>ic.VideoDuration) and (InterviewStatus=2 or InterviewStatus=4)
    union
    Select im.id as InterviewId, Case when imj.Joinedon is null and imc.PerformedOn is null then 'No Show'
    when imj.Joinedon is null then 'Interviewer No Show' when imc.PerformedOn is null then 'Candidate No Show' end as Type,'Fake Report' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    outer apply(Select max(Joinedon) Joinedon from InterviewMeetingsJoinTime where Interviewid=im.id) imj
    outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc
    where DateDiff(Day,im.interviewDate,Getdate())<30 and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)
    and (imj.Joinedon is null or imc.PerformedOn is null) and ic.MeetingStatus!=5 and ic.MeetingStatus!=1 and ic.MeetingStatus!=9 and ic.MeetingStatus!=2 and ic.MeetingStatus!=3
  ) x GROUP BY InterviewId) d ON d.InterviewId = im.id
  where im.interviewDate >= @startDate AND im.interviewDate < @endDate and ulr.EmailId=@EmailId                       
                            
 End                            
                            
else if(@EmailId is not null and ((select idProfile from UserList where EmailId=@EmailId)=31))                                   
 Begin                            
  select distinct im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration, ulc.EmailId as candidateEmail,                           
  im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, IRF.JobCountry, IRF.CalendlyLink,                                    
  im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime,                         
  case                         
  when ulc.LastName is null then ulc.FirstName                        
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                        
  end as candidateName,                        
  im.currentEmployerName,                                       
  im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore, im.SessionId,IRF.IsAIInterview,                                      
  IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.CalendlyLink,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,                                
  ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId                                     
  ,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline,                            
  im.HRCoordinatorEmailId, im.HRComment, cl.[Name] as Client,                            
  qcfr.QCStatus,ulcu.EmailId as BillingOwner, ic.VideoDuration,  ca.ResumeLink as resumeLink,                  
  ca.Phone as MobileNumber, ca.ClientHRAttemptContact as CandidateClientHR, irf.IsQCRequired,im.IsCandidateResponsed ,im.CandidateResponse,im.ProctorReport,im.Cancellationsource                               
  , itp.ResumeLink as interviewerresumeLink, itp.CodedResumeLink as encodedresumeLink,ca.IsProctor, im.ZoomMeetingId            
  , im.HasDistinctParticipants, irf.CreatedFromTemplateId, csc.[Name] as TemplateName,IRF.IsAadhaarVerificationRequired,          
  ulc.AadhaarVerified,im.RedoInterview,ISNULL(im.MeetingPlatform,'Zoom') as MeetingPlatform,          
  im.BackUpLink,im.BackUpLinkId,  ulcho.EmailId as ChannelOwner,IRF.IsVideoRequired,           
  Case When IRF.IsNewSkillChannel=1 and ti.Surcharge=0 then 1 else 0 end as IsSurcharge ,itp.isVerified,itp.IsEmployed,itp.CompanyName,itp.EmploymentComment,itp.RedMarker                 
  --,si.Uniqueid as SchedulerUniqueid,ul.EmailId as CandidateEmailId      
  ,iman.interviewDate RescheduleDate,ti.PerSuccessRate, ic.AudioTranscriptFileLink, ic.IsAITranscriptAnalysisAttempted, im.AssistantId, im.SessionId, irf.IsAIInterview
  , d.NonAIreasoning, d.Riskcategory
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
  outer Apply(Select top(1)ima.interviewDate From InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where im.IRFId=ima.IRFId and im.candidateEmail=ima.candidateEmail and ima.confirmationStatus=1 order by ima.Interviewdate desc) iman      
  --left join UserList ul on ul.id = cm.UserId      
  --left join ScheduleInterviewEvent si on si.SkillChannel=IRF.IRFId      
  LEFT JOIN (SELECT InterviewId, STRING_AGG(Type, ',') AS NonAIreasoning, STRING_AGG(Riskcategory, ',') AS Riskcategory FROM (
    Select im.id as InterviewId,'deficitmorethan10' as Type,'High Severity' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType
    where (MeetingStatus=4) and DateDiff(Day,im.interviewDate,Getdate())<30
    and (ic.VideoDuration-pt.DurationSelectedMin)>(-pt.DurationSelectedMin) and (ic.VideoDuration-pt.DurationSelectedMin)<-10
    union
    Select im.id as InterviewId,'AIVerdictLessthan4' as Type,'High Severity' as Riskcategory
    From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join Client cl on cl.id=IRF.ClientId
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30
    and ic.AITranscriptAnalysisScore<4
    union
    Select im.id as InterviewId,'CandidateRating' as Type,'High Severity' as Riskcategory
    From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join Client cl on cl.id=IRF.ClientId
    join CandidateFeedbackDetails cfd on cfd.InterviewId=im.id
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30 and cfd.Rating<4
    union
    Select im.id as InterviewId,'Selectdeficit' as Type,'Under Review' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30
    and (pt.DurationSelectedMin*2/3>ic.VideoDuration) and (InterviewStatus=2 or InterviewStatus=4)
    union
    Select im.id as InterviewId, Case when imj.Joinedon is null and imc.PerformedOn is null then 'No Show'
    when imj.Joinedon is null then 'Interviewer No Show' when imc.PerformedOn is null then 'Candidate No Show' end as Type,'Fake Report' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    outer apply(Select max(Joinedon) Joinedon from InterviewMeetingsJoinTime where Interviewid=im.id) imj
    outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc
    where DateDiff(Day,im.interviewDate,Getdate())<30 and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)
    and (imj.Joinedon is null or imc.PerformedOn is null) and ic.MeetingStatus!=5 and ic.MeetingStatus!=1 and ic.MeetingStatus!=9 and ic.MeetingStatus!=2 and ic.MeetingStatus!=3
  ) x GROUP BY InterviewId) d ON d.InterviewId = im.id
  where im.interviewDate >= @startDate AND im.interviewDate < @endDate and ulsh.EmailId =@EmailId                           
 End                            
else                            
 Begin                            
  select distinct im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration, ulc.EmailId as candidateEmail,                           
  im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, IRF.JobCountry,IRF.CalendlyLink,                                     
  im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime,                         
  case                   
  when ulc.LastName is null then ulc.FirstName                        
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                        
  end as candidateName,                        
  im.currentEmployerName,                                       
  im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore, im.SessionId,IRF.IsAIInterview,                                      
  IRF.id as irfIntId,IRF.JDLink as JDLink,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,                                
  ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId                                     
  ,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline,                            
  im.HRCoordinatorEmailId, im.HRComment, cl.[Name] as Client,                            
  qcfr.QCStatus,ulcu.EmailId as BillingOwner, ic.VideoDuration,  ca.ResumeLink as resumeLink,                  
  ca.Phone as MobileNumber, ca.ClientHRAttemptContact as CandidateClientHR, irf.IsQCRequired,im.IsCandidateResponsed  ,im.CandidateResponse,im.ProctorReport,im.Cancellationsource                              
  , itp.ResumeLink as interviewerresumeLink, itp.CodedResumeLink as encodedresumeLink,ca.IsProctor, im.ZoomMeetingId             
  , im.HasDistinctParticipants, irf.CreatedFromTemplateId, csc.[Name] as TemplateName,IRF.IsAadhaarVerificationRequired,          
  ulc.AadhaarVerified,im.RedoInterview,ISNULL(im.MeetingPlatform,'Zoom') as MeetingPlatform,          
  im.BackUpLink,im.BackUpLinkId,  ulcho.EmailId as ChannelOwner, IRF.IsVideoRequired, IRF.CalendlyLink,         
  Case When IRF.IsNewSkillChannel=1 and ti.Surcharge=0 then 1 else 0 end as IsSurcharge,itp.isVerified,itp.IsEmployed,itp.CompanyName,itp.EmploymentComment,itp.RedMarker            
 --,si.Uniqueid as SchedulerUniqueid      
 ,iman.interviewDate RescheduleDate ,ti.PerSuccessRate, ic.AudioTranscriptFileLink, ic.IsAITranscriptAnalysisAttempted, im.AssistantId, im.SessionId, irf.IsAIInterview
 , d.NonAIreasoning, d.Riskcategory
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
  outer Apply(Select top(1)ima.interviewDate From InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where im.IRFId=ima.IRFId and im.candidateEmail=ima.candidateEmail and ima.confirmationStatus=1 order by ima.Interviewdate desc) iman      
  --right join ScheduleInterviewEvent si on si.SkillChannel=IRF.IRFId      
  LEFT JOIN (SELECT InterviewId, STRING_AGG(Type, ',') AS NonAIreasoning, STRING_AGG(Riskcategory, ',') AS Riskcategory FROM (
    Select im.id as InterviewId,'deficitmorethan10' as Type,'High Severity' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType
    where (MeetingStatus=4) and DateDiff(Day,im.interviewDate,Getdate())<30
    and (ic.VideoDuration-pt.DurationSelectedMin)>(-pt.DurationSelectedMin) and (ic.VideoDuration-pt.DurationSelectedMin)<-10
    union
    Select im.id as InterviewId,'AIVerdictLessthan4' as Type,'High Severity' as Riskcategory
    From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join Client cl on cl.id=IRF.ClientId
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30
    and ic.AITranscriptAnalysisScore<4
    union
    Select im.id as InterviewId,'CandidateRating' as Type,'High Severity' as Riskcategory
    From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join Client cl on cl.id=IRF.ClientId
    join CandidateFeedbackDetails cfd on cfd.InterviewId=im.id
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30 and cfd.Rating<4
    union
    Select im.id as InterviewId,'Selectdeficit' as Type,'Under Review' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    join IRF on IRF.IRFId=im.IRFId
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30
    and (pt.DurationSelectedMin*2/3>ic.VideoDuration) and (InterviewStatus=2 or InterviewStatus=4)
    union
    Select im.id as InterviewId, Case when imj.Joinedon is null and imc.PerformedOn is null then 'No Show'
    when imj.Joinedon is null then 'Interviewer No Show' when imc.PerformedOn is null then 'Candidate No Show' end as Type,'Fake Report' as Riskcategory
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id
    outer apply(Select max(Joinedon) Joinedon from InterviewMeetingsJoinTime where Interviewid=im.id) imj
    outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc
    where DateDiff(Day,im.interviewDate,Getdate())<30 and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)
    and (imj.Joinedon is null or imc.PerformedOn is null) and ic.MeetingStatus!=5 and ic.MeetingStatus!=1 and ic.MeetingStatus!=9 and ic.MeetingStatus!=2 and ic.MeetingStatus!=3
  ) x GROUP BY InterviewId) d ON d.InterviewId = im.id
  where im.interviewDate >= @startDate AND im.interviewDate < @endDate             
 End          
                                       
End                     
                 
GO
