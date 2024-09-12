IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetDayStatsByClientUser'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDayStatsByClientUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetDayStatsByClientUser    
@startDate datetime,    
@endDate datetime,    
@timeNow datetime,    
@emailid varchar(150)    
AS    
BEGIN    
Declare @AllScheduleinterview as Table(    
interviewdate datetime,    
id int,    
candidateEmail varchar(150),    
confirmationStatus int,  
IRFId varchar(15)  
)    
Declare @ClientRole int
Declare @Clientid int
Select @ClientRole=RoleId,@Clientid=ClientId From ClientUser cu join USerList ul on ul.id=Cu.UserId where ul.Emailid= 'danijel.j@turing.com' 

if(@ClientRole=9)
begin
Insert into @AllScheduleinterview    
Select interviewDate,im.id,im.candidateEmail,im.confirmationStatus,IRF.IRFId From InterviewMeetings im    
join IRF on IRF.IRFId=im.IRFId    
where interviewDate>Convert(date,@startDate) and interviewDate<Convert(date,@endDate) and IRF.Clientid=@Clientid
End
else
begin
Insert into @AllScheduleinterview    
Select interviewDate,im.id,im.candidateEmail,im.confirmationStatus,IRF.IRFId From InterviewMeetings im    
join IRF on IRF.IRFId=im.IRFId    
join TransClientUser tc on tc.IRFId=IRF.id    
join UserList ul on ul.id=tc.ClientUserId    
where interviewDate>Convert(date,@startDate) and interviewDate<Convert(date,@endDate) and ul.EmailId=@emailid    
end
Select Convert(date,interviewdate) as interviewdate,COUNT(im.id) as CandidateCount,Count(icc.id) as CompletedInterviews,Count(ics.id) as SelectedMeetings,Count(icr.id) as RejectedMeetings,Count(icrs.id) as ReScheduledCount,Count(icrf.id) as FutureScheduledMeetings,Count(icrp.id) as PendingCount,IRF.IRFId,  
Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,IRF.id,sie.Concurrency,IRF.InterviewDuration  
From @AllScheduleinterview im  
join IRF on IRF.IRFId=im.IRFId  
left join InterviewComments icc on im.id=icc.InterviewId and (icc.MeetingStatus=4 or icc.MeetingStatus=6 or icc.MeetingStatus=7 or icc.MeetingStatus=8)    
left join InterviewComments ics on im.id=ics.InterviewId and (ics.InterviewStatus=2 or icc.InterviewStatus=4)    
left join InterviewComments icr on im.id=icr.InterviewId and (icr.InterviewStatus=3 or icr.InterviewStatus=5)    
left join InterviewComments icrs on im.id=icrs.InterviewId and (im.confirmationStatus=2 or icrs.MeetingStatus=1 or icrs.MeetingStatus=3 or icrs.MeetingStatus=5 or icrs.MeetingStatus=9)    
left join InterviewComments icrf on im.id=icrf.InterviewId and im.confirmationStatus=1 and im.interviewdate>@timeNow    
left join InterviewComments icrp on im.id=icrp.InterviewId and im.confirmationStatus=1 and im.interviewdate<@timeNow and (icrp.MeetingStatus=2 or icrp.MeetingStatus is null)    
left join ScheduleInterviewEvent sie on sie.SkillChannel=IRF.IRFId
group by Convert(date,interviewdate),IRF.IRFId,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end,IRF.id,sie.Concurrency,IRF.InterviewDuration      
END    
Go