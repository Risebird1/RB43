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
confirmationStatus int
)

Insert into @AllScheduleinterview
Select interviewDate,im.id,im.candidateEmail,im.confirmationStatus From InterviewMeetings im
join IRF on IRF.IRFId=im.IRFId
join TransClientUser tc on tc.IRFId=IRF.id
join UserList ul on ul.id=tc.ClientUserId
where interviewDate>Convert(date,@startDate) and interviewDate<Convert(date,@endDate) and ul.EmailId=@emailid

Select Convert(date,im.interviewdate) as interviewdate,COUNT(im.id) as CandidateCount,Count(icc.id) as CompletedInterviews,Count(ics.id) as SelectedMeetings,Count(icr.id) as RejectedMeetings,Count(icrs.id) as ReScheduledCount,Count(icrf.id) as FutureScheduledMeetings,Count(icrp.id) as PendingCount,Count(cci.id) as ColdCandidateCount
From @AllScheduleinterview im
join InterviewMeetings ima on im.id=ima.id join IRF on IRF.IRFId=ima.IRFId 
join UserList ul on ul.EmailId=ima.candidateEmail 
join CandidatesMaster cm on cm.UserId=ul.id
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id
left join ColdCandidateInfos cci on cci.FKCandidateId=ca.id and ISNULL(cci.IsColdCandidate,0)=1
left join InterviewComments icc on im.id=icc.InterviewId and (icc.MeetingStatus=4 or icc.MeetingStatus=6 or icc.MeetingStatus=7 or icc.MeetingStatus=8) and ISNULL(cci.IsColdCandidate,0)=0
left join InterviewComments ics on im.id=ics.InterviewId and (ics.InterviewStatus=2 or icc.InterviewStatus=4) and ISNULL(cci.IsColdCandidate,0)=0
left join InterviewComments icr on im.id=icr.InterviewId and (icr.InterviewStatus=3 or icr.InterviewStatus=5) and ISNULL(cci.IsColdCandidate,0)=0
left join InterviewComments icrs on im.id=icrs.InterviewId and (im.confirmationStatus=2 or icrs.MeetingStatus=1 or icrs.MeetingStatus=3 or icrs.MeetingStatus=5 or icrs.MeetingStatus=9) and ISNULL(cci.IsColdCandidate,0)=0
left join InterviewComments icrf on im.id=icrf.InterviewId and im.confirmationStatus=1 and im.interviewdate>@timeNow and ISNULL(cci.IsColdCandidate,0)=0
left join InterviewComments icrp on im.id=icrp.InterviewId and im.confirmationStatus=1 and im.interviewdate<@timeNow and (icrp.MeetingStatus=2 or icrp.MeetingStatus is null) and ISNULL(cci.IsColdCandidate,0)=0
group by Convert(date,im.interviewdate)
END
Go