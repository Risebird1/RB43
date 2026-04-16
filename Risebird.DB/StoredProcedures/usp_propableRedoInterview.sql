IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_propableRedoInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_propableRedoInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_propableRedoInterview      
@emailid varchar(200),    
@actionstatus varchar(50)    
AS      
BEGIN      
Declare @currentdate datetime=DateAdd(Minute,330,Getdate())    
if(@actionstatus='PropableRedo')    
begin    
;with CTE as(Select im.id,'deficitmorethan10' as Type      
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
join IRF on IRF.IRFId=im.IRFId      
join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType      
where (MeetingStatus=4) and DateDiff(Day,im.interviewDate,Getdate())<30      
and (ic.VideoDuration-pt.DurationSelectedMin)>(-pt.DurationSelectedMin) and (ic.VideoDuration-pt.DurationSelectedMin)<-10      
union      
--AI Transcript score <6      
Select im.id,'AIVerdictLessthan4' as Type      
From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
join IRF on IRF.IRFId=im.IRFId      
join Client cl on cl.id=IRF.ClientId      
where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30      
and ic.AITranscriptAnalysisScore<4      
union      
--Candidate Rating <4      
Select im.id,'CandidateRating' as Type      
From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
join IRF on IRF.IRFId=im.IRFId      
join Client cl on cl.id=IRF.ClientId      
join CandidateFeedbackDetails cfd on cfd.InterviewId=im.id      
where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30  and cfd.Rating<4)      
SELECT STRING_AGG(CTE.Type, ', ') AS StatusValue,im.IRFId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,CASE  WHEN DATEDIFF(MINUTE, im.interviewDate,imjf.FirstJoinedon) >= 5 THEN 1  ELSE 0  END AS InterviewerLateJoin,IRF.IsVideoRequired,uli.MobileNumber as InterviewerMobileNumber,imjt.Joinedon,imjf.FirstJoinedon,ulco.EmailId as ChannelOwner,pm.DurationSelectedMin as interviewDuration,ic.AITranscriptVerdict,ic.AudioTranscriptFileLink,ic.AITranscriptAnalysisScore,ic.AIVerdict,cfd.Rating,Isnull(ulim.FirstName,'') as InterviewerName,(pm.DurationSelectedMin-ic.VideoDuration) as deficit       
FROM CTE join InterviewMeetings im on im.id=CTE.id join IRF on im.IRFId=irf.IRFId      
join Client cl on irf.ClientId=cl.id       
join InterviewComments ic on ic.InterviewId=im.id       
join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1       
join UserList ulim on ulim.EmailId=im.interviewer      
left join UserList ulco on IRF.RisebirdHr=ulco.id      
left join CandidateFeedbackDetails cfd on cfd.Interviewid=im.id       
left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.CompanyType=IRF.CompanyType and pm.PlanTypeEnumValue=IRF.Heuristicsscore      
Outer Apply(Select Max(Joinedon) Joinedon from  InterviewMeetingsJoinTime       
left join UserList ulco on IRF.RisebirdHr=ulco.id where im.interviewer=interviewer and im.id=interviewid) imjt       
Outer Apply(Select min(Joinedon) FirstJoinedon from  InterviewMeetingsJoinTime where im.id=interviewid) imjf        
where IsNull(IsProctor,0)=0 and ISNULL(im.RedoInterview,0)=0 and tq.id is null    
GROUP BY im.IRFId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id,IRF.id,CASE  WHEN DATEDIFF(MINUTE, im.interviewDate,imjf.FirstJoinedon) >= 5 THEN 1  ELSE 0  END ,IRF.IsVideoRequired,uli.MobileNumber,imjt.Joinedon,imjf.FirstJoinedon,ulco.EmailId,pm.DurationSelectedMin,ic.AITranscriptVerdict,ic.AudioTranscriptFileLink,ic.AITranscriptAnalysisScore,ic.AIVerdict,cfd.Rating,Isnull(ulim.FirstName,''),(pm.DurationSelectedMin-ic.VideoDuration)       
order by Interviewdate desc      
end    
else if(@actionstatus='QCUnderReview')    
begin    
;with CTE as(Select im.id,'Selectdeficit' as Type      
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
join IRF on IRF.IRFId=im.IRFId      
join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType      
where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30      
and (pt.DurationSelectedMin*2/3>ic.VideoDuration) and (InterviewStatus=2 or InterviewStatus=4)    
union    
Select im.id,'Rejectdeficit' as Type      
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
join IRF on IRF.IRFId=im.IRFId      
join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType      
where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30      
and (pt.DurationSelectedMin*4/9>ic.VideoDuration) and (InterviewStatus=1 or InterviewStatus=3 or InterviewStatus=5))    
SELECT STRING_AGG(CTE.Type, ', ') AS StatusValue,im.IRFId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,CASE  WHEN DATEDIFF(MINUTE, im.interviewDate,imjf.FirstJoinedon) >= 5 THEN 1  ELSE 0  END AS InterviewerLateJoin,IRF.IsVideoRequired,uli.MobileNumber as InterviewerMobileNumber,imjt.Joinedon,imjf.FirstJoinedon,ulco.EmailId as ChannelOwner,pm.DurationSelectedMin as interviewDuration,ic.AITranscriptVerdict,ic.AudioTranscriptFileLink,ic.AITranscriptAnalysisScore,ic.AIVerdict,cfd.Rating,Isnull(ulim.FirstName,'') as InterviewerName,(pm.DurationSelectedMin-ic.VideoDuration) as deficit       
FROM CTE join InterviewMeetings im on im.id=CTE.id join IRF on im.IRFId=irf.IRFId      
join Client cl on irf.ClientId=cl.id       
join InterviewComments ic on ic.InterviewId=im.id       
join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1       
join UserList ulim on ulim.EmailId=im.interviewer      
left join UserList ulco on IRF.RisebirdHr=ulco.id      
left join CandidateFeedbackDetails cfd on cfd.Interviewid=im.id       
left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.CompanyType=IRF.CompanyType and pm.PlanTypeEnumValue=IRF.Heuristicsscore      
Outer Apply(Select Max(Joinedon) Joinedon from  InterviewMeetingsJoinTime       
left join UserList ulco on IRF.RisebirdHr=ulco.id where im.interviewer=interviewer and im.id=interviewid) imjt       
Outer Apply(Select min(Joinedon) FirstJoinedon from  InterviewMeetingsJoinTime where im.id=interviewid) imjf        
where IsNull(IsProctor,0)=0 and ISNULL(im.RedoInterview,0)=0 and tq.id is null    
GROUP BY im.IRFId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id,IRF.id,CASE  WHEN DATEDIFF(MINUTE, im.interviewDate,imjf.FirstJoinedon) >= 5 THEN 1  ELSE 0  END ,IRF.IsVideoRequired,uli.MobileNumber,imjt.Joinedon,imjf.FirstJoinedon,ulco.EmailId,pm.DurationSelectedMin,ic.AITranscriptVerdict,ic.AudioTranscriptFileLink,ic.AITranscriptAnalysisScore,ic.AIVerdict,cfd.Rating,Isnull(ulim.FirstName,''),(pm.DurationSelectedMin-ic.VideoDuration)       
order by Interviewdate desc     
end    
else if(@actionstatus='QCAIInterview')    
begin    
SELECT im.IRFId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,    
ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,    
CASE  WHEN DATEDIFF(MINUTE, im.interviewDate,imjf.FirstJoinedon) >= 5 THEN 1  ELSE 0  END AS InterviewerLateJoin,    
IRF.IsVideoRequired,uli.MobileNumber as InterviewerMobileNumber,imjt.Joinedon,imjf.FirstJoinedon,ulco.EmailId as ChannelOwner,    
pm.DurationSelectedMin as interviewDuration,ic.AITranscriptVerdict,ic.AudioTranscriptFileLink,ic.AITranscriptAnalysisScore,    
ic.AIVerdict,cfd.Rating,(pm.DurationSelectedMin-ic.VideoDuration) as deficit,    
IRF.IsAIInterview, im.SessionId, a.VideoCount as AICandidateResponseVideoCount       
FROM InterviewMeetings im join IRF on im.IRFId=irf.IRFId      
join Client cl on irf.ClientId=cl.id       
join InterviewComments ic on ic.InterviewId=im.id       
join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1       
left join UserList ulco on IRF.RisebirdHr=ulco.id      
left join CandidateFeedbackDetails cfd on cfd.Interviewid=im.id       
left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.CompanyType=IRF.CompanyType and pm.PlanTypeEnumValue=IRF.Heuristicsscore      
Outer Apply(Select Max(Joinedon) Joinedon from  InterviewMeetingsJoinTime       
left join UserList ulco on IRF.RisebirdHr=ulco.id where im.interviewer=interviewer and im.id=interviewid) imjt       
Outer Apply(Select min(Joinedon) FirstJoinedon from  InterviewMeetingsJoinTime where im.id=interviewid) imjf      
cross apply (select count(id) as VideoCount from BotInterviewQuestionVideos where SessionId = im.SessionId) a    
where IsNull(IsProctor,0)=0 and ISNULL(im.RedoInterview,0)=0 and tq.id is null and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and IRF.IsAIInterview=1 and interviewDate<@currentdate and DATEDIFF(DAY, interviewDate,@currentdate)<7    
GROUP BY im.IRFId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,    
ic.InterviewStatus,cl.Name,im.id,IRF.id,CASE  WHEN DATEDIFF(MINUTE, im.interviewDate,imjf.FirstJoinedon) >= 5 THEN 1  ELSE 0  END ,    
IRF.IsVideoRequired,uli.MobileNumber,imjt.Joinedon,imjf.FirstJoinedon,ulco.EmailId,pm.DurationSelectedMin,ic.AITranscriptVerdict,    
ic.AudioTranscriptFileLink,ic.AITranscriptAnalysisScore,ic.AIVerdict,    
cfd.Rating,(pm.DurationSelectedMin-ic.VideoDuration),    
IRF.IsAIInterview, im.SessionId, a.VideoCount     
order by Interviewdate desc     
end
else if(@actionstatus='QCSelect')
begin
Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,ic.AIVerdict,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,uli.MobileNumber as InterviewerMobileNumber,ulco.EmailId as ChannelOwner
from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
join InterviewComments ic on ic.InterviewId=im.id 
join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id 
join Candidate ca on ca.CandidateMasterId=cm.id 
left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 
left join UserList ulco on IRF.RisebirdHr=ulco.id
where IsNull(IsProctor,0)=0 and DateDiff(Day,im.interviewDate,Getdate())<30 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ISNULL(im.RedoInterview,0)=0 and ISNULL(IRF.IsAIInterview,0)=0
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=2 or InterviewStatus=4)
end
END    
GO