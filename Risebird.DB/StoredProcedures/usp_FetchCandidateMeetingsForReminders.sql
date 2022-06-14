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
Select IRF.id as Irfid,im.id as Interviewid,candidateEmail as CandidateEmail,im.interviewDate,ul.FirstName,2 as Typefunction,cm.id candidateid,ul.uniqueid,im.staffingAgency,im.MobileNumber                 
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId left join Userlist ul on ul.EmailId=im.candidateEmail               
left join CandidatesMaster cm on cm.UserId=ul.id left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id             
where interviewDate>=dateadd(minute,420,GetDate()) and interviewDate<=dateadd(minute,450,GetDate()) and confirmationstatus=1 and Isnull(IsCandidateResponsed,0)=0 and ISNULL(cl.IsCandidateavailability,0)=1 and IRF.CalendlyLink like '%InterviewScheduler/ScheduleEvent%'              
union all                
--For 6 hr                
Select IRF.id as Irfid,im.id as Interviewid,candidateEmail as CandidateEmail,im.interviewDate,ul.FirstName,6 as Typefunction,cm.id candidateid,ul.uniqueid,im.staffingAgency,im.MobileNumber        
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId left join Userlist ul on ul.EmailId=im.candidateEmail               
left join CandidatesMaster cm on cm.UserId=ul.id left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id              
where interviewDate>=dateadd(minute,660,GetDate()) and interviewDate<=dateadd(minute,690,GetDate()) and confirmationstatus=1 and Isnull(IsCandidateResponsed,0)=0 and ISNULL(cl.IsCandidateavailability,0)=1 and IRF.CalendlyLink like '%InterviewScheduler/ScheduleEvent%'               
union all                
--For 24 hr                
Select IRF.id as Irfid,im.id as Interviewid,candidateEmail as CandidateEmail,im.interviewDate,ul.FirstName,24 as Typefunction,cm.id candidateid,ul.uniqueid,im.staffingAgency,im.MobileNumber        
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId left join Userlist ul on ul.EmailId=im.candidateEmail              
left join CandidatesMaster cm on cm.UserId=ul.id left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id             
where interviewDate>=dateadd(minute,1740,GetDate()) and interviewDate<=dateadd(minute,1770,GetDate()) and confirmationstatus=1 and Isnull(IsCandidateResponsed,0)=0 and ISNULL(cl.IsCandidateavailability,0)=1 and IRF.CalendlyLink like '%InterviewScheduler/ScheduleEvent%'              
  
END     
 
 Go
  



  