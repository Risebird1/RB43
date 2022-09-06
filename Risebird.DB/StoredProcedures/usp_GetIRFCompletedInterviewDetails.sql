IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIRFCompletedInterviewDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFCompletedInterviewDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetIRFCompletedInterviewDetails
@startDate datetime,          
@endDate datetime,                  
@timeNow datetime,
@id int,
@type int
AS          
BEGIN          

DECLARE @TemporaryCandidateTableVariable TABLE (                    
 IRFId int,              
 CandidateMasterId int,              
 UserId int,         
 CandidateLatestStatus varchar(20)              
);                 
   
If(@type!=64868)
BEGIN
INSERT INTO @TemporaryCandidateTableVariable                    
select ca.IRFId, ca.CandidateMasterId, cm.UserId,                
dbo.fn_CandidateLatestStatusForIrfStats(cm.UserId, ca.IRFId, cast(@startDate as date), @timeNow)              
from  Candidate ca               
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)               
join CandidatesMaster cm on cm.id=ca.CandidateMasterId                
where cs.[Status] =12 and ca.IRFId=@id and cast([Date] as date) <=cast(@endDate as date) and cast([Date] as date) >= cast(@startDate as date)              
group by ca.IRFId, ca.CandidateMasterId,cm.UserId              
END

if(@type=34657) --Reschedule     
BEGIN
Select im.interviewDate,im.interviewer,im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,im.confirmationStatus,ul.MobileNumber
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join InterviewComments ic on ic.Interviewid=im.id 
join UserList ul on ul.Emailid=im.CandidateEmail 
join @TemporaryCandidateTableVariable tc on tc.UserId=ul.id
where IRF.id=@id and (im.confirmationStatus=2 or ic.MeetingStatus=1 or ic.MeetingStatus=3 or ic.MeetingStatus=5)       
END
Else If(@type=64868)--Not Schedule
BEGIN
Select Distinct ul.EmailId as candidateEmail,ul.FirstName as candidateName,ul.MobileNumber,ca.ClientHRAttemptContact 
From Candidate ca join CandidatesMaster cm on cm.id = ca.CandidateMasterId 
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)               
join UserList ul on ul.id = cm.UserId left join InterviewMeetings im on im.candidateEmail = ul.EmailId 
where cs.[Status] =12 and ca.IRFId=@id and cast([Date] as date) <=cast(@endDate as date) and cast([Date] as date) >= cast(@startDate as date) and im.confirmationStatus is null
END
else
begin
;With CTE as(Select interviewer,Count(im.id) TotalInterview,MeetingStatus,InterviewStatus,inp.NickName,ul.id as Interviewerid 
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id 
join UserList ul on ul.EmailId=im.interviewer join InterviewerProfile inp on inp.UserId=ul.id
join USerList uli on uli.EmailId=im.candidateEmail
join IRF on IRF.IRFId=im.IRFId 
join @TemporaryCandidateTableVariable tc on tc.UserId=uli.id
where ic.MeetingStatus in(4,6,7,8) and IRF.id=@id 
Group by Interviewer,MeetingStatus,InterviewStatus,inp.NickName,ul.id) 
SELECT interviewer,NickName,Interviewerid as InterviewerProfileId,Sum(TotalInterview) TotalInterview,
SUM(Case when (InterviewStatus=1 or InterviewStatus=3)  then TotalInterview else 0 end) AS Reject,
SUM(Case when InterviewStatus=2 or InterviewStatus=4 then TotalInterview else 0 end) AS Selected 
FROM CTE 
group by interviewer,NickName,Interviewerid
end
END
GO