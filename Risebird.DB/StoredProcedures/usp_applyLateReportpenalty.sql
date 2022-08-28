IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_applyLateReportpenalty'))
BEGIN
    DROP PROCEDURE dbo.usp_applyLateReportpenalty
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_applyLateReportpenalty          
AS           
BEGIN          
Insert into InterviewPoints(Interviewid,userid,Points,Type,Comment,Status,Date,InterviewerId,PaymentHistoryId)           
Select im.id as Interviewid,ul.id as userid,Case When irf.JobCountry=1 then -200 when irf.JobCountry=2 then -350 end as Points,'Penalty' as Type,'Late Report '+im.candidateEmail as Comment,'Pending' as Status,GETDATE() as Date,0 as InterviewerId,0 as PaymentHistoryId            
from InterviewMeetings im join irf on irf.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId           
left join Userlist ul on ul.EmailId=im.interviewer          
left join UserList ulc on ulc.EmailId=im.candidateEmail left join CandidatesMaster cm on cm.UserId=ulc.id           
left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=irf.id           
where interviewDate>=dateadd(minute,-1470,GetDate()) and interviewDate<dateadd(minute,-1410,GetDate()) and im.confirmationStatus=1 and ic.MeetingStatus=2 and IsNull(ca.IsProctor,0)=0          
      
Update InterviewComments set MeetingStatus=5 where interviewid in(Select im.id from InterviewMeetings im join irf on irf.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId           
left join Userlist ul on ul.EmailId=im.interviewer          
left join UserList ulc on ulc.EmailId=im.candidateEmail left join CandidatesMaster cm on cm.UserId=ulc.id           
left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=irf.id           
where interviewDate>=dateadd(minute,-9750,GetDate()) and interviewDate<dateadd(minute,-9690,GetDate()) and im.confirmationStatus=1 and ic.MeetingStatus=2 and IsNull(ca.IsProctor,0)=0)          

Insert into InterviewPoints(Interviewid,userid,Points,Type,Comment,Status,Date,InterviewerId,PaymentHistoryId)            
Select im.id as Interviewid,ul.id as userid,Case When irf.JobCountry=1 then -200 when irf.JobCountry=2 then -350 end as Points,'Interview' as Type,'Penalty '+im.candidateEmail as Comment,'Pending' as Status,GETDATE() as Date,0 as InterviewerId,0 as PaymentHistoryId            
from InterviewMeetings im join irf on irf.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId           
left join Userlist ul on ul.EmailId=im.interviewer          
left join UserList ulc on ulc.EmailId=im.candidateEmail left join CandidatesMaster cm on cm.UserId=ulc.id           
left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=irf.id           
where ic.MeetingStatus=5 and interviewDate>=dateadd(minute,-9750,GetDate()) and interviewDate<dateadd(minute,-9690,GetDate()) and im.confirmationStatus=1 and ic.MeetingStatus=2 and IsNull(ca.IsProctor,0)=0

      
Delete from InterviewPoints where interviewid in(Select Interviewid From InterviewPoints where interviewid in(Select ip.Interviewid from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id         
join Userlist ul on ul.EmailId=im.interviewer        
join InterviewPoints ip on ip.Interviewid=im.id and ip.userid=ul.id        
where im.interviewDate>=dateadd(minute,-9870,GetDate()) and im.interviewDate<dateadd(minute,-9750,GetDate())         
and ic.MeetingStatus=5 and ip.Type='Penalty' and ip.Comment like '%Late Report%')      
and Type='Interview')and Type='Penalty' and Comment like '%Late Report%'        
          
END

GO