IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchMissingParticipantFakeReporting'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchMissingParticipantFakeReporting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchMissingParticipantFakeReporting 
@actionstatus varchar(30),  
@emailid varchar(200),  
@currentdate datetime,
@StartDate datetime=null,
@EndDate datetime=null
AS
BEGIN
Declare @startDateParam datetime
Declare @endDateParam datetime
if(@StartDate is null)
begin
Set @startDateParam=DateAdd(Day,-8,Getdate())
end
else
begin
Set @startDateParam=@StartDate
end
if(@EndDate  is null)
begin
Set @endDateParam=DateAdd(Minute,330,Getdate())
end
else
begin
Set @endDateParam=@EndDate
end
;with CTE as(Select Distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,im.SessionId,IRF.IsAIInterview,
CASE  WHEN DATEDIFF(MINUTE, im.interviewDate,imjf.FirstJoinedon) >= 5 THEN 1  ELSE 0  END AS InterviewerLateJoin,IRF.IsVideoRequired,ulca.MobileNumber as InterviewerMobileNumber,
pm.DurationSelectedMin as interviewDuration,ic.AITranscriptVerdict,ic.AudioTranscriptFileLink,ic.AIVerdict,imj.Joinedon,imc.PerformedOn,
Case when imj.Joinedon is null and imc.PerformedOn is null then 'No Show' 
when imj.Joinedon is null then 'Interviewer No Show' 
when imc.PerformedOn is null then 'Candidate No Show' end as StatusValue,ic.AITranscriptAnalysisScore,cfd.Rating,
ulc.EmailId as ChannelOwner,Isnull(ulim.FirstName,'') as InterviewerName,(pm.DurationSelectedMin-ic.VideoDuration) as deficit
from InterviewMeetings im                  
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId       
left join UserList ulca on ulca.EmailId=im.candidateEmail                    
left join CandidatesMaster cm on cm.UserId=ulca.id                    
left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id      
Left join InterviewComments ic on ic.InterviewId=im.id                
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid            
left join UserList ulc on ulc.id=IRF.RisebirdHr  
left join CandidateFeedbackDetails cfd on cfd.Interviewid=im.id 
join UserList ulim on ulim.EmailId=im.interviewer
left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.CompanyType=IRF.CompanyType and pm.PlanTypeEnumValue=IRF.Heuristicsscore
outer apply(Select max(Joinedon) Joinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imj          
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc          
outer apply(Select min(Joinedon) FirstJoinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imjf          
outer apply(Select min(PerformedOn) FirstPerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imcf          
outer Apply(Select top(1)ima.interviewDate,ica.MeetingStatus From InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where im.IRFId=ima.IRFId and im.candidateEmail=ima.candidateEmail and ima.confirmationStatus=1 order by ima.Interviewdate desc) iman      
where im.InterviewDate>@startDateParam and im.InterviewDate<@endDateParam and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission) and IsAIInterview=0 and (imj.Joinedon is null or imc.PerformedOn is null))
Select * From CTE where MeetingStatus!=5 and MeetingStatus!=1 and MeetingStatus!=9 and MeetingStatus!=2
END
GO