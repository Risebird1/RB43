IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_applyLateReportpenalty'))
BEGIN
    DROP PROCEDURE dbo.usp_applyLateReportpenalty
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_applyLateReportpenalty
AS 
BEGIN
Insert into InterviewPoints(Interviewid,userid,Points,Type,Comment,Status,Date,InterviewerId,PaymentHistoryId) 
Select im.id as Interviewid,ul.id as userid,Case When irf.JobCountry=1 then -200 when irf.JobCountry=2 then -350 end as Points,'Penalty' as Type,'Late Report '+im.candidateEmail as Comment,'Pending' as Status,GETDATE() as Date,0 as InterviewerId,0 as PaymentHistoryId  
from InterviewMeetings im join irf on irf.IRFId=im.IRFId join InterviewComments ic on im.id=ic.InterviewId 
left join Userlist ul on ul.EmailId=im.interviewer
left join UserList ulc on ulc.EmailId=im.candidateEmail left join CandidatesMaster cm on cm.UserId=ulc.id 
left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=irf.id 
where interviewDate>=dateadd(minute,-270,GetDate()) and interviewDate<dateadd(minute,-210,GetDate()) and im.confirmationStatus=1 and ic.MeetingStatus=2 and IsNull(ca.IsProctor,0)=0

END
GO