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
--Start : Late report penalty  
DECLARE @tempInterviews TABLE (            
    id int,  
 Penalty int,  
 Userid int  
)     
DECLARE @UpdatedInterviews TABLE (            
    id int,  
 Userid int  
)     
--Interviews with no report between 4 hrs to 24 hrs (50%)  
Insert into @tempInterviews  
Select im.id,50 as Penalty,ul.id as Userid From InterviewMeetings im join irf on irf.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId               
join Userlist ul on ul.EmailId=im.interviewer   
left join UserList ulc on ulc.EmailId=im.candidateEmail  
left join CandidatesMaster cm on cm.UserId=ulc.id               
left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=irf.id               
where interviewDate>=dateadd(minute,-1110,GetDate()) and interviewDate<dateadd(minute,90,GetDate()) and im.confirmationStatus=1 and ic.MeetingStatus=2 and IsNull(ca.IsProctor,0)=0              
--Interviews with no report after 24 hrs (100%)  
Union  
Select im.id,100 as Penalty,ul.id as Userid From InterviewMeetings im join irf on irf.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId               
join Userlist ul on ul.EmailId=im.interviewer   
left join UserList ulc on ulc.EmailId=im.candidateEmail  
left join CandidatesMaster cm on cm.UserId=ulc.id               
left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=irf.id               
where interviewDate<dateadd(minute,-1110,GetDate()) and interviewDate>dateadd(minute,-1170,GetDate()) and im.confirmationStatus=1 and ic.MeetingStatus=2 and IsNull(ca.IsProctor,0)=0              


Update ip set ip.PenaltyAmount=tp.Penalty   
Output INSERTED.interviewid as id,INSERTED.Userid into @UpdatedInterviews  
from InterviewPoints ip join @tempInterviews tp on ip.InterviewId=tp.id and ip.userid=tp.Userid and ip.Type='Interview' and Status='Pending'  

Insert into InterviewPoints(Interviewid,userid,Points,Type,Comment,Status,Date,InterviewerId,PaymentHistoryId,PenaltyAmount)               
Select im.id as Interviewid,ul.id as userid,0 as Points,'Interview' as Type,im.candidateEmail as Comment,'Pending' as Status,GETDATE() as Date,0 as InterviewerId,0 as PaymentHistoryId,tp.Penalty as PenaltyAmount               
from @tempInterviews tp join 
InterviewMeetings im on tp.id=im.id    
join Userlist ul on ul.EmailId=im.interviewer   
where tp.id NOT in (Select id from @UpdatedInterviews)  
--End : Late report penalty  
          
Update InterviewComments set MeetingStatus=5 where interviewid in(Select im.id from InterviewMeetings im join irf on irf.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId               
left join Userlist ul on ul.EmailId=im.interviewer              
left join UserList ulc on ulc.EmailId=im.candidateEmail left join CandidatesMaster cm on cm.UserId=ulc.id               
left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=irf.id               
where interviewDate>=dateadd(minute,-9750,GetDate()) and interviewDate<dateadd(minute,-9690,GetDate()) and im.confirmationStatus=1 and ic.MeetingStatus=2 and IsNull(ca.IsProctor,0)=0)              
  
--Start : Update Penalty on Noshow  
DECLARE @NoShowInterview TABLE (            
    id int,  
 Userid int,  
 Points int  
)   
DECLARE @UpdatedNoShowInterview TABLE (            
    id int,  
 Userid int  
)   
  
Insert into @NoShowInterview  
Select im.id ,ul.id as Userid,Case When irf.JobCountry=1 then -200 when irf.JobCountry=2 then -350 end as Points  
from InterviewMeetings im join irf on irf.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId               
left join Userlist ul on ul.EmailId=im.interviewer              
left join UserList ulc on ulc.EmailId=im.candidateEmail left join CandidatesMaster cm on cm.UserId=ulc.id               
left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=irf.id               
where ic.MeetingStatus=5 and interviewDate>=dateadd(minute,-9750,GetDate()) and interviewDate<dateadd(minute,-9690,GetDate())   
and im.confirmationStatus=1 and IsNull(ca.IsProctor,0)=0    
  
Update ip set ip.Points=tns.Points  
Output INSERTED.interviewid as id,INSERTED.Userid into @UpdatedNoShowInterview  
from InterviewPoints ip join @NoShowInterview tns on ip.InterviewId=tns.id and ip.userid=tns.Userid and ip.Type='Interview' and Status='Pending'  
  
Insert into InterviewPoints(Interviewid,userid,Points,Type,Comment,Status,Date,InterviewerId,PaymentHistoryId,PenaltyAmount)               
Select im.id as Interviewid,ul.id as userid,tns.Points as Points,'Interview' as Type,im.candidateEmail as Comment,'Pending' as Status,GETDATE() as Date,0 as InterviewerId,0 as PaymentHistoryId,0 as PenaltyAmount  
from InterviewMeetings im   
join Userlist ul on ul.EmailId=im.interviewer   
join @NoShowInterview tns on tns.id=im.id  
where tns.id NOT in (Select id from @UpdatedNoShowInterview)  
--End : Update Penalty on Noshow    
  
Update ip set PenaltyAmount=0 from InterviewPoints ip join InterviewMeetings im on im.id=ip.Interviewid join InterviewComments ic on ic.InterviewId=ip.Interviewid  
join UserList ul on ul.EmailId=im.interviewer and ip.userid=ul.id where (ic.MeetingStatus=5 or ic.MeetingStatus=9) and Isnull(ip.PenaltyAmount,0)>0 and interviewDate>=dateadd(minute,-9750,GetDate()) and interviewDate<dateadd(minute,-9690,GetDate())   
  
--Start: Will Delete after 15 days          
Delete from InterviewPoints where interviewid in(Select Interviewid From InterviewPoints where interviewid in(Select ip.Interviewid from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id             
join Userlist ul on ul.EmailId=im.interviewer            
join InterviewPoints ip on ip.Interviewid=im.id and ip.userid=ul.id            
where ip.Status='Pending'  
and ic.MeetingStatus=5 and ip.Type='Penalty' and ip.Comment like '%Late Report%')          
and Type='Interview')and Type='Penalty' and Comment like '%Late Report%'            
--End: Will Delete after 15 days              
END  

GO