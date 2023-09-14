  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerInsights'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerInsights
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetInterviewerInsights
@Emailid varchar(150)
AS
BEGIN
Declare @TotalInterviews int
Declare @CompletedInterviews int
Declare @SelectedInterviews int
Declare @RejectedInterviews int
Declare @NoShowInterviews int
Declare @NoShowCandidateInterviews int
Declare @DroppedInterviews int
Select @TotalInterviews=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid 


Select @CompletedInterviews=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid 
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)

Select @SelectedInterviews=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid 
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=2 or InterviewStatus=4)

Select @RejectedInterviews=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid 
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=3 or InterviewStatus=1)

Select @NoShowInterviews=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid 
and (MeetingStatus=5 or MeetingStatus=9)

Select @NoShowCandidateInterviews=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid 
and MeetingStatus=1

Select @DroppedInterviews=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid 
and MeetingStatus=3

Select ul.id as Userid,ul.Emailid,ip.CompanyName,ip.IsEmployed,ul.MobileNumber,ul.LastLogin,ip.VideoConfirmation,ip.Isavailable,ip.EmploymentComment,Sum(ti.Point) as ReliabilityScore,
@TotalInterviews as TotalInterviews,@CompletedInterviews as CompletedInterviews,@SelectedInterviews as SelectedInterviews,@RejectedInterviews as RejectedInterviews,
@NoShowInterviews as NoShowInterviews,@NoShowCandidateInterviews as NoShowCandidateInterviews,@DroppedInterviews as DroppedInterviews
from InterviewerProfile ip join UserList ul on ul.id=ip.Userid 
left join TransInterviewersReliabilityScores ti on ti.InterviewerId=ip.id
where ul.EmailId=@Emailid
Group by ul.id,ul.Emailid,ip.CompanyName,ip.IsEmployed,ul.MobileNumber,ul.LastLogin,ip.VideoConfirmation,ip.Isavailable,ip.EmploymentComment

END
Go