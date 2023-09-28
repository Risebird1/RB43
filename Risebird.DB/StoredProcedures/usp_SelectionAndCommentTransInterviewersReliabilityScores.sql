/****** Object:  StoredProcedure [dbo].[usp_SelectionAndCommentTransInterviewersReliabilityScores]    Script Date: 9/23/2023 06:46:23 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SelectionAndCommentTransInterviewersReliabilityScores'))
BEGIN
    DROP PROCEDURE dbo.usp_SelectionAndCommentTransInterviewersReliabilityScores
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure [dbo].[usp_SelectionAndCommentTransInterviewersReliabilityScores]
@InterviewMeetingId int,
@CreateDate datetime
as
begin 
declare @InterviewerId int    
declare @Interviewer varchar(100)    
declare @InterviewerStatus int    
declare @MeetingStatus int    
  
set @Interviewer=(select im.Interviewer from InterviewMeetings im join UserList ul on im.interviewer=ul.EmailId where im.id=@InterviewMeetingId)    
    
set @InterviewerStatus=(Select Isnull(InterviewStatus,0) as Interviewstatus from  InterviewComments ic  where interviewid=@InterviewMeetingId)  
    
set @InterviewerId=(select ip.Id from InterviewerProfile ip join UserList ul on ip.UserId=ul.Id join InterviewMeetings im on ul.EmailId=im.interviewer where Im.id= @InterviewMeetingId)    
  
declare @interviewCommentLength int    
set @interviewCommentLength=(Select Len(ic.InterviewerComment) from InterviewComments ic  where interviewid=@InterviewMeetingId)    
  
if(@InterviewerStatus=2 or @InterviewerStatus=4)    
begin    
if(@interviewCommentLength<500)    
begin        
exec [dbo].[usp_TransInterviewersReliabilityScores] @InterviewMeetingId,20,@CreateDate,@Interviewer  
end    
if(@interviewCommentLength>500)    
begin         
exec [dbo].[usp_TransInterviewersReliabilityScores] @InterviewMeetingId,21,@CreateDate,null  
end    
end    
else if(@InterviewerStatus=3 or @InterviewerStatus=1)    
begin    
if(@interviewCommentLength<250)    
begin        
exec [dbo].[usp_TransInterviewersReliabilityScores] @InterviewMeetingId,18,@CreateDate,null  
end    
if(@interviewCommentLength>250)    
begin    
exec [dbo].[usp_TransInterviewersReliabilityScores] @InterviewMeetingId,19,@CreateDate,null  
end    
end    
set @MeetingStatus= (select MeetingStatus from InterviewComments ic  where interviewid=@InterviewMeetingId)    
if(@MeetingStatus=5 or @MeetingStatus=9)    
begin       
exec [dbo].[usp_TransInterviewersReliabilityScores] @InterviewMeetingId,13,@CreateDate,null  
end    
--Less than 5 or more than 5 Questions added in the Report    
declare @QuestionCount int    
set @QuestionCount=(select Count(id) from QuestionsDiscussed where interviewid=@InterviewMeetingId)    
if(@QuestionCount<5)    
begin        
exec [dbo].[usp_TransInterviewersReliabilityScores] @InterviewMeetingId,16,@CreateDate,null  
end    
if(@QuestionCount>5)    
begin        
exec [dbo].[usp_TransInterviewersReliabilityScores]@InterviewMeetingId,17,@CreateDate,null  
end      
declare @CompletedInterviews int    
 set @CompletedInterviews=(Select Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Interviewer     
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8))    
declare @SelectedInterview int    
set @SelectedInterview=(select Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Interviewer     
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=2 or InterviewStatus=4))    
    
if((@SelectedInterview*100/@CompletedInterviews)<20 or (@SelectedInterview*100/@CompletedInterviews)>40)    
begin        
exec [dbo].[usp_TransInterviewersReliabilityScores] 0,14,@CreateDate,@Interviewer  
end    
else if((@SelectedInterview*100/@CompletedInterviews)>=20 and (@SelectedInterview*100/@CompletedInterviews)<=40)    
begin       
exec [dbo].[usp_TransInterviewersReliabilityScores] 0,15,@CreateDate,@Interviewer  
end   
  
declare @LoginTimeCount int  
set @LoginTimeCount=(select top 1 DateDIFF(MINUTE,im.interviewDate,imjt.Joinedon)  from InterviewMeetingsJoinTime imjt join  
InterviewMeetings im   on imjt.interviewid= im.id where im.id=@InterviewMeetingId order by imjt.Joinedon desc)  
if(@LoginTimeCount>=5)  
begin    
exec [dbo].[usp_TransInterviewersReliabilityScores] @InterviewMeetingId,6,@CreateDate,null  
end   
End


