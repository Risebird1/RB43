IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AutoApprovePaymentSlaDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AutoApprovePaymentSlaDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GOCreate Procedure usp_AutoApprovePaymentSlaDetailsASBEGIN;with CTE as(Select ps.*,IRF.IRFId,IRF.id as irfIntId,im.candidateEmail,im.interviewer,im.interviewDate,ul.EmailId as ChannelOwner,ip.CompanyName, ip.ResumeLink, ip.RedMarker, uli.MobileNumber,im.MobileNumber as CandidatePhone,RTRIM(LTRIM((ISNULL(uli.FirstName,'')+' '+ISNULL(uli.LastName,'')))) as InterviewerName,tq.Redflags,Case when tq.id>0 then 1 else 0 end as QCStatus,    imj.Joinedon,im.candidateName,psh.TotalPaymentComplianceCount,im.SessionId,IRF.IsAIInterview,Case when ISNull(ps.IsApproved,0)=1 then 'Approved' when ISNull(ps.IsApproved,0)=2 then 'Not Approved' else '' end as StatusValue,IRF.PriorityLevel,d.NonAIreasoning,d.Riskcategory,ic.InterviewStatus     From PaymentSLADetails ps left join InterviewMeetings im on ps.Interviewid=im.id     left join InterviewComments ic on ic.Interviewid=im.idleft join IRF on IRF.IRFId=im.IRFId     left join UserList ul on ul.id=IRF.RisebirdHr    left join UserList uli on uli.EmailId=im.interviewer    left join InterviewerProfile ip on ip.UserId=uli.id    left join Trans_QCFormReport tq on tq.interviewid=im.id LEFT JOIN (SELECT InterviewId,STRING_AGG(Type, ',') AS NonAIreasoning,STRING_AGG(Riskcategory, ',') AS Riskcategory FROM (
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
where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30  and cfd.Rating<4
union
Select im.id as InterviewId,'Selectdeficit' as Type,'Under Review' as Riskcategory        
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
join IRF on IRF.IRFId=im.IRFId      
join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType      
where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30      
and (pt.DurationSelectedMin*2/3>ic.VideoDuration) and (InterviewStatus=2 or InterviewStatus=4)    
union
Select im.id as InterviewId,'Rejectdeficit' as Type,'Under Review' as Riskcategory              
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
join IRF on IRF.IRFId=im.IRFId      
join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType      
where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and DateDiff(Day,im.interviewDate,Getdate())<30      
and (pt.DurationSelectedMin*4/9>ic.VideoDuration) and (InterviewStatus=1 or InterviewStatus=3 or InterviewStatus=5)
union
Select qca.InterviewId,'AIRedo' as Type,'Redo Requested' as Riskcategory        
from QcInterviewAIevaluation qca where ISNULL(AIMergeRedoInterview,0)=1
union
Select InterviewId,'Report AI Score' as Type,'High Severity' as RiskcategoryFrom InterviewComments where AIReportQCAnalysisScore<60UnionSelect InterviewId,'Transcript AI Score' as Type,'High Severity' as RiskcategoryFrom InterviewComments where AITranscriptAnalysisScore<6
UnionSelect qca.InterviewId,'Contradiction AI Score' as Type,'High Severity' as Riskcategoryfrom QcInterviewAIevaluation qca where ISNULL(TRY_CAST(ContradictionScore AS DECIMAL(10,2)), 0) <=5
union
Select im.id as InterviewId,Case when imj.Joinedon is null and imc.PerformedOn is null then 'No Show'   
when imj.Joinedon is null then 'Interviewer No Show' when imc.PerformedOn is null then 'Candidate No Show' end as Type,'Fake Report' as Riskcategory
from InterviewMeetings im  join InterviewComments ic on ic.InterviewId=im.id                  
outer apply(Select max(Joinedon) Joinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imj            
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc            
where DateDiff(Day,im.interviewDate,Getdate())<30 and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission) and (imj.Joinedon is null or imc.PerformedOn is null) and ic.MeetingStatus!=5 and ic.MeetingStatus!=1 and ic.MeetingStatus!=9 and ic.MeetingStatus!=2 and ic.MeetingStatus!=3)x GROUP BY InterviewId) d ON d.InterviewId = im.idouter apply(Select Min(Joinedon) Joinedon from InterviewMeetingsJoinTime where interviewid=im.id) as imjouter apply(Select Count(id) as TotalPaymentComplianceCount  From PaymentSLADetailsHistory where Interviewid=ps.Interviewid) as pshwhere ISNull(ps.IsApproved,0)=0 and (ic.InterviewStatus=4 or ic.InterviewStatus=2 or ic.InterviewStatus=1 or ic.InterviewStatus=3  or ic.InterviewStatus=5))Select * From CTE where (Riskcategory is null and (InterviewStatus=2 or InterviewStatus=4) and PriorityLevel not in(2,3)) or((Riskcategory IS NULL or Riskcategory not in('Fake Report','Under Review')) and (InterviewStatus=1 or InterviewStatus=3 or InterviewStatus=5))ENDGO