  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerInsights'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerInsights
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetInterviewerInsights
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
Declare @PaymentSLA int
Declare @Penalty int
Declare @CandidateRating decimal
Declare @CustomerRating decimal
Declare @Redorequests int
Declare @RedFlags int

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

Select @PaymentSLA=Count(ps.id) from PaymentSLADetails ps join Interviewmeetings im on im.id=ps.Interviewid where im.interviewer=@Emailid

Select @Penalty=Count(ip.id) from InterviewPoints ip join Interviewmeetings im on im.id=ip.Interviewid where PenaltyAmount>0 or Comment like '%Late Report%' and im.interviewer=@Emailid

Select @CandidateRating=Avg(Question2) From CandidateRatings cr join Interviewmeetings im on im.id=cr.Interviewid where im.interviewer=@Emailid

Select @CustomerRating=avg(Rating) From ClientCommentsForReport cr join Interviewmeetings im on im.id=cr.Interviewid where im.interviewer=@Emailid

Select @Redorequests=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and RedoInterview=1 and  interviewer=@Emailid 

Select @RedFlags=Count(tq.id) from Trans_QCFormReport tq join Interviewmeetings im  on im.id=tq.InterviewId where confirmationStatus=1 and Redflags>0 and  interviewer=@Emailid 

Select ul.id as Userid,ul.Emailid,ip.CompanyName,ip.IsEmployed,ul.MobileNumber,ul.LastLogin,ip.VideoConfirmation,ip.Isavailable,ip.EmploymentComment,Sum(ti.Point) as ReliabilityScore,
@TotalInterviews as TotalInterviews,@CompletedInterviews as CompletedInterviews,@SelectedInterviews as SelectedInterviews,@RejectedInterviews as RejectedInterviews,
@NoShowInterviews as NoShowInterviews,@NoShowCandidateInterviews as NoShowCandidateInterviews,@DroppedInterviews as DroppedInterviews,Isnull(ul.FirstName,'') as interviewerName,
Case When Isnull(si.id,0)>0 then 1 else 0 end as IsSuspicious,@PaymentSLA as PaymentSLA,@Penalty as Penalty,@CandidateRating as CandidateRating,@CustomerRating as CustomerRating,
@Redorequests as Redorequests,@RedFlags as RedFlags
from InterviewerProfile ip join UserList ul on ul.id=ip.Userid 
left join TransInterviewersReliabilityScores ti on ti.InterviewerId=ip.id
left join SuspiciousInterviewer si on si.UserId=ul.id and si.SuspiciousStatus=1
where ul.EmailId=@Emailid
Group by ul.id,ul.Emailid,ip.CompanyName,ip.IsEmployed,ul.MobileNumber,ul.LastLogin,ip.VideoConfirmation,ip.Isavailable,ip.EmploymentComment,Isnull(ul.FirstName,''),Case When Isnull(si.id,0)>0 then 1 else 0 end

END


Go