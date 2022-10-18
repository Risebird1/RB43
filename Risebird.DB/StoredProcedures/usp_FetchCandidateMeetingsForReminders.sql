IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchCandidateMeetingsForReminders'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchCandidateMeetingsForReminders
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO              
CREATE Procedure usp_FetchCandidateMeetingsForReminders                  
As                      
BEGIN                      
--For 2 hr                  
Select IRF.id as Irfid,im.id as Interviewid,candidateEmail as CandidateEmail,im.interviewDate,ul.FirstName,2 as Typefunction,cm.id candidateid,ul.uniqueid,im.staffingAgency,im.MobileNumber,cl.Name as ClientName,IRF.PrimarySkillInvite                      
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId left join Userlist ul on ul.EmailId=im.candidateEmail                       
left join CandidatesMaster cm on cm.UserId=ul.id left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id                     
where interviewDate>=dateadd(minute,420,GetDate()) and interviewDate<=dateadd(minute,450,GetDate()) and confirmationstatus=1 and Isnull(IsCandidateResponsed,0)=0 and ISNULL(Case When IRF.IsCandidateavailability is null and IRF.IsMeetingNotificationSend is
null then null Else IRF.IsCandidateavailability end,cl.IsCandidateavailability)=1     
union all                        
--For 6 hr                        
Select IRF.id as Irfid,im.id as Interviewid,candidateEmail as CandidateEmail,im.interviewDate,ul.FirstName,6 as Typefunction,cm.id candidateid,ul.uniqueid,im.staffingAgency,im.MobileNumber,cl.Name as ClientName,IRF.PrimarySkillInvite                
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId left join Userlist ul on ul.EmailId=im.candidateEmail                       
left join CandidatesMaster cm on cm.UserId=ul.id left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id                      
where interviewDate>=dateadd(minute,660,GetDate()) and interviewDate<=dateadd(minute,690,GetDate()) and confirmationstatus=1 and Isnull(IsCandidateResponsed,0)=0 and ISNULL(Case When IRF.IsCandidateavailability is null and IRF.IsMeetingNotificationSend is null then null Else IRF.IsCandidateavailability end,cl.IsCandidateavailability)=1     
union all                        
--For 24 hr                        
Select IRF.id as Irfid,im.id as Interviewid,candidateEmail as CandidateEmail,im.interviewDate,ul.FirstName,24 as Typefunction,cm.id candidateid,ul.uniqueid,im.staffingAgency,im.MobileNumber,cl.Name as ClientName,IRF.PrimarySkillInvite                
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId left join Userlist ul on ul.EmailId=im.candidateEmail                      
left join CandidatesMaster cm on cm.UserId=ul.id left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id                     
where interviewDate>=dateadd(minute,1740,GetDate()) and interviewDate<=dateadd(minute,1770,GetDate()) and confirmationstatus=1 and Isnull(IsCandidateResponsed,0)=0 and ISNULL(Case When IRF.IsCandidateavailability is null and IRF.IsMeetingNotificationSend is null then null Else IRF.IsCandidateavailability end,cl.IsCandidateavailability)=1     
union all                        
--For 1 hr cancelled                       
Select IRF.id as Irfid,im.id as Interviewid,candidateEmail as CandidateEmail,im.interviewDate,ul.FirstName,1 as Typefunction,cm.id candidateid,ul.uniqueid,im.staffingAgency,im.MobileNumber,cl.Name as ClientName,IRF.PrimarySkillInvite                
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId left join Userlist ul on ul.EmailId=im.candidateEmail                      
left join CandidatesMaster cm on cm.UserId=ul.id left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id                     
where interviewDate>=dateadd(minute,360,GetDate()) and interviewDate<=dateadd(minute,390,GetDate()) and confirmationstatus=1 and Isnull(IsCandidateResponsed,0)=0 and ISNULL(Case When IRF.IsCandidateavailability is null and IRF.IsMeetingNotificationSend is null then null Else IRF.IsCandidateavailability end,cl.IsCandidateavailability)=1  and ISNULL(Case When IRF.IsCandidateavailability is null and IRF.IsMeetingNotificationSend is null then null Else IRF.IsMeetingAutoCancelled end,cl.IsMeetingAutoCancelled)=1   
--For meeting reminder set 0  
union all  
Select IRF.id as Irfid,im.id as Interviewid,candidateEmail as CandidateEmail,im.interviewDate,ul.FirstName,31 as Typefunction,cm.id candidateid,ul.uniqueid,im.staffingAgency,im.MobileNumber,cl.Name as ClientName,IRF.PrimarySkillInvite                      
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId left join Userlist ul on ul.EmailId=im.candidateEmail                       
left join CandidatesMaster cm on cm.UserId=ul.id left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id                     
where interviewDate>=dateadd(minute,420,GetDate()) and interviewDate<=dateadd(minute,450,GetDate()) and confirmationstatus=1 and Isnull(IsCandidateResponsed,0)=0 and 
ISNULL(Case When IRF.IsCandidateavailability is null and IRF.IsMeetingNotificationSend is null then null Else IRF.IsCandidateavailability end,cl.IsCandidateavailability)=0    
    
END 
 
 Go
  



  