IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_TotalInterviewerInterviewDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_TotalInterviewerInterviewDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_TotalInterviewerInterviewDetails  
@startdate datetime,  
@enddate datetime  
as  
begin  
Select ul.EmailId as interviewer,ul.MobileNumber,ip.NickName,ip.ResumeLink,ip.CompanyName,ul.id as UserId,     
tc.TotalCompletedMeetings,sti.TotalInterview,Fti.TotalFuture,ip.IsEmployed,ip.CompanyName,ip.EmploymentComment,  
Ftis.TotalAssignedFuture as SkillChannelFutureAssignedInterview,tsv.TranscriptVerdit,tsc.TotalSelectMeetings,trc.TotalRejectMeetings,tdf.TotalDeficitMeetings,  
Case when ip.VideoLink is not null then 'yes' else 'No' end VideoKYC,ip.VideoLink,ip.Isavailable,ip.AvailabilityResponseDate,ip.RedMarkerStatus,Rcd.RedMarkerReviewComment,ul.IsActive  
From UserList ul join InterviewerProfile ip on ip.UserId=ul.id  
outer Apply (Select Count(im.id) as TotalCompletedMeetings from InterviewMeetings im join InterviewComments ic on ic.Interviewid=im.id where im.Interviewer=ul.EmailId and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)) tc      
outer Apply (Select Count(id) as TotalFuture from InterviewMeetings where interviewer=ul.EmailId and interviewDate>DATEADD(MINUTE,330,Getdate())) Fti      
outer Apply (Select Count(id) as TotalInterview from InterviewMeetings where interviewer=ul.EmailId) sti      
outer Apply (Select Count(id) as TotalAssignedFuture from InterviewMeetings where interviewer=ul.EmailId and interviewDate>DATEADD(MINUTE,330,Getdate())) Ftis      
outer Apply (Select Count(im.id) as TranscriptVerdit from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id where interviewer=ul.EmailId and ic.AITranscriptVerdict='yes') tsv  
outer Apply (Select Count(im.id) as TotalSelectMeetings from InterviewMeetings im join InterviewComments ic on ic.Interviewid=im.id where im.Interviewer=ul.EmailId and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) 
and (ic.InterviewStatus=2 or ic.InterviewStatus=4)) tsc      
outer Apply (Select Count(im.id) as TotalRejectMeetings from InterviewMeetings im join InterviewComments ic on ic.Interviewid=im.id where im.Interviewer=ul.EmailId and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) 
and (ic.InterviewStatus=1 or ic.InterviewStatus=5 or ic.InterviewStatus=3)) trc  
outer Apply (Select Count(im.id) as TotalDeficitMeetings from InterviewMeetings im join InterviewComments ic on ic.Interviewid=im.id join IRF ir on im.IRFId=ir.IRFId join PlanTypeChargesMaster pt on pt.CompanyType=ir.CompanyType and pt.PlanTypeEnumValue=ir.Heuristicsscore and pt.CountryType=ir.JobCountry   
where (ic.VideoDuration-pt.DurationSelectedMin<0 and ic.VideoDuration-pt.DurationSelectedMin>-pt.DurationSelectedMin) and im.Interviewer=ul.EmailId and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)) tdf  
outer Apply (Select Top 1 Comment as RedMarkerReviewComment from RedMarkerStatusDetails where InterviewerUserid=ul.id and IRFId=0 and Type='TotalInterview' Order by CreatedOn desc) Rcd
where EmailId in(Select Distinct Interviewer from InterviewMeetings where interviewDate>@startdate and interviewDate<@enddate and Interviewer is not null)  
order by sti.TotalInterview desc  
end
GO