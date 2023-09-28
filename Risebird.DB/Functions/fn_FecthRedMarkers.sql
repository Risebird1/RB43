
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_FecthRedMarkers]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_FecthRedMarkers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
       
CREATE FUNCTION fn_FecthRedMarkers (@Emailid varchar(200))              
RETURNS int          
as          
Begin          
declare @redflagMarker int =0       
  
Declare @TotalInterviews int          
Declare @CompletedInterviews int          
Declare @SelectedInterviews int          
Declare @RejectedInterviews int          
Declare @NoShowInterviews int          
Declare @NoShowCandidateInterviews int          
Declare @RedFlags int          
Declare @Redorequests int          
Declare @SuspiciousCount int          
Declare @UnassignInterviews int          
Declare @JoinLateMeeting int
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
          
Select @RedFlags=Count(tq.id) from Trans_QCFormReport tq join Interviewmeetings im  on im.id=tq.InterviewId where confirmationStatus=1 and Redflags>0 and  interviewer=@Emailid           
          
Select @Redorequests=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and RedoInterview=1 and  interviewer=@Emailid           
          
Select @SuspiciousCount=Count(si.Id) From SuspiciousInterviewer si join UserList ul on ul.id=si.UserId where SuspiciousStatus=1 and ul.EmailId=@Emailid          
          
Select @UnassignInterviews=Count(ip.id) From InterviewPoints ip join InterviewMeetings im on im.id=ip.Interviewid join InterviewComments ic on ic.Interviewid=im.id Left join UserList ul on ul.id=ip.userid          
where points<0 and ic.MeetingStatus!=5 and ic.MeetingStatus!=2 and ul.EmailId=@Emailid           
          
Select @JoinLateMeeting=Count(imj.id) from InterviewMeetingsJoinTime imj join InterviewMeetings im on im.id=imj.interviewid where DATEDIFF(MINUTE,interviewDate,Joinedon)>5 and im.interviewer=@Emailid           

Select @SelectedComment=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid     
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=2 or InterviewStatus=4)  and Len(ic.InterviewerComment)<500   
  
Select @RejectedComment=Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Emailid     
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=3 or InterviewStatus=1) and Len(ic.InterviewerComment)<250   
  
 ;with CTE(Interviewid,totalQuestions) as  (select qd.Interviewid,Count(qd.id) as totalQuestions from QuestionsDiscussed qd join InterviewMeetings im on im.id=qd.InterviewId  group by qd.Interviewid having Count(qd.id)<5)  
 Select @LessQuestions=Count(im.id) From CTE join Interviewmeetings im on im.id=CTE.Interviewid join InterviewComments ic on im.id=ic.InterviewId   
where confirmationStatus=1 and  interviewer=@Emailid   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)  
          
--Start: Completed Interview greter than 0          
if(@CompletedInterviews>0)          
BEGIN          
if((@SelectedInterviews*100/@CompletedInterviews)>=50)          
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END          
          
if((@RejectedInterviews*100/@CompletedInterviews)>=80)          
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END          
          
if((@RedFlags*100/@CompletedInterviews)>10)          
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END          
END          
--END: Completed Interview greter than 0          
--Start: Total Interview greter than 0          
If(@TotalInterviews>0)          
BEGIN          
if((@NoShowInterviews*100/@TotalInterviews)>5)          
BEGIN       
Set @redflagMarker=@redflagMarker+1          
END          
          
if((@NoShowCandidateInterviews*100/@TotalInterviews)>40)          
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END          
          
if((@UnassignInterviews*100/@TotalInterviews)>5)        
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END          
          
if((@JoinLateMeeting*100/@TotalInterviews)>5)          
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END          
END          
--END: Total Interview greter than 0          
if(@Redorequests>0)          
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END          
          
if(@SuspiciousCount>0)          
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END 

if(@RejectedComment>0)          
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END 

if(@SelectedComment>0)          
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END 

if(@LessQuestions>0)          
BEGIN          
Set @redflagMarker=@redflagMarker+1          
END 
          
Return @redflagMarker;          
END     

GO