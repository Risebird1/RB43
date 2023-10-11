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
Declare @PaymentSLA int  
Declare @Penalty int  
Declare @CandidateRating decimal  
Declare @CustomerRating decimal  
Declare @Redorequests int  
Declare @RedFlags int  
Declare @JoinLateMeeting int,@JoinLateCandidateNoShow int   
Declare @UnassignInterviews int  
Declare @SuspiciousCount int  
Declare @RejectedComment int 
Declare @SelectedComment int 
Declare @LessQuestions int 
  
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
  
Select @Penalty=Count(ip.id) from InterviewPoints ip join Interviewmeetings im on im.id=ip.Interviewid where (PenaltyAmount>0 or Comment like '%Late Report%') and im.interviewer=@Emailid  
  
Select @CandidateRating=Avg(Question2) From CandidateRatings cr join Interviewmeetings im on im.id=cr.Interviewid where im.interviewer=@Emailid  
  
Select @CustomerRating=avg(Rating) From ClientCommentsForReport cr join Interviewmeetings im on im.id=cr.Interviewid where im.interviewer=@Emailid  
  
Select @Redorequests=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and RedoInterview=1 and  interviewer=@Emailid   
  
Select @RedFlags=Count(tq.id) from Trans_QCFormReport tq join Interviewmeetings im  on im.id=tq.InterviewId where confirmationStatus=1 and Redflags>0 and  interviewer=@Emailid   
  
Select @JoinLateMeeting=Count(imj.id) from InterviewMeetingsJoinTime imj join InterviewMeetings im on im.id=imj.interviewid where DATEDIFF(MINUTE,interviewDate,Joinedon)>5 and im.interviewer=@Emailid   
  
Select @JoinLateCandidateNoShow=Count(imj.id) from InterviewMeetingsJoinTime imj join InterviewMeetings im on im.id=imj.interviewid join InterviewComments ic on ic.Interviewid=im.id   
where DATEDIFF(MINUTE,interviewDate,Joinedon)>5 and ic.MeetingStatus=1 and im.interviewer=@Emailid   
  
Select @UnassignInterviews=Count(ip.id) From InterviewPoints ip join InterviewMeetings im on im.id=ip.Interviewid join InterviewComments ic on ic.Interviewid=im.id Left join UserList ul on ul.id=ip.userid  
where points<0 and ic.MeetingStatus!=5 and ic.MeetingStatus!=2 and ul.EmailId=@Emailid   
  
Select @SuspiciousCount=Count(si.Id) From SuspiciousInterviewer si join UserList ul on ul.id=si.UserId where SuspiciousStatus=1 and ul.EmailId=@Emailid  

Select @SelectedComment=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=2 or InterviewStatus=4)  and Len(ic.InterviewerComment)<500 

Select @RejectedComment=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=3 or InterviewStatus=1) and Len(ic.InterviewerComment)<250 

 ;with CTE(Interviewid,totalQuestions) as  (select qd.Interviewid,Count(qd.id) as totalQuestions from QuestionsDiscussed qd join InterviewMeetings im on im.id=qd.InterviewId  group by qd.Interviewid having Count(qd.id)<5)
 Select @LessQuestions=Count(im.id) From CTE join Interviewmeetings im on im.id=CTE.Interviewid join InterviewComments ic on im.id=ic.InterviewId 
where confirmationStatus=1 and  interviewer=@Emailid 
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)
  
Select ul.id as Userid,ul.Emailid,ip.CompanyName,ip.IsEmployed,ul.MobileNumber,ul.LastLogin,ip.VideoConfirmation,ip.Isavailable,ip.EmploymentComment,Sum(ti.Point) as ReliabilityScore,ip.ResumeLink as ResumeLink,
@TotalInterviews as TotalInterviews,@CompletedInterviews as CompletedInterviews,@SelectedInterviews as SelectedInterviews,@RejectedInterviews as RejectedInterviews,  
@NoShowInterviews as NoShowInterviews,@NoShowCandidateInterviews as NoShowCandidateInterviews,@DroppedInterviews as DroppedInterviews,Isnull(ul.FirstName,'') as interviewerName,  
Case When Isnull(si.id,0)>0 then 1 else 0 end as IsSuspicious,@PaymentSLA as PaymentSLA,@Penalty as Penalty,@CandidateRating as CandidateRating,@CustomerRating as CustomerRating,  
@Redorequests as Redorequests,@RedFlags as RedFlags,@JoinLateMeeting as JoinLateMeeting,@JoinLateCandidateNoShow as JoinLateCandidateNoShow,@UnassignInterviews as UnassignInterviews,@SuspiciousCount as SuspiciousCount,
@SelectedComment as SelectedComment,@RejectedComment as RejectedComment,@LessQuestions as LessQuestions,ul.uniqueid
from InterviewerProfile ip join UserList ul on ul.id=ip.Userid   
left join TransInterviewersReliabilityScores ti on ti.InterviewerId=ip.id  
left join SuspiciousInterviewer si on si.UserId=ul.id and si.SuspiciousStatus=1  
where ul.EmailId=@Emailid  
Group by ul.id,ul.Emailid,ip.CompanyName,ip.IsEmployed,ul.MobileNumber,ul.LastLogin,ip.VideoConfirmation,ip.Isavailable,ip.EmploymentComment,Isnull(ul.FirstName,''),Case When Isnull(si.id,0)>0 then 1 else 0 end,ul.uniqueid,ip.ResumeLink
  
END  
  
Go