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
declare @Point int  
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
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=20)  
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,20,@Point,@CreateDate)  
end  
if(@interviewCommentLength>500)  
begin   
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=21)  
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,21,@Point,@CreateDate)  
end  
end  
else if(@InterviewerStatus=3 or @InterviewerStatus=1)  
begin  
if(@interviewCommentLength<250)  
begin  
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=18)  
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,18,@Point,@CreateDate)  
end  
if(@interviewCommentLength>250)  
begin  
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=19)  
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,19,@Point,@CreateDate)  
end  
end  

set @MeetingStatus= (select MeetingStatus from InterviewComments ic  where interviewid=@InterviewMeetingId)  
if(@MeetingStatus=5 or @MeetingStatus=9)  
begin  
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=13)  
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,13,@Point,@CreateDate)  
end  

--Less than 5 or more than 5 Questions added in the Report  
declare @QuestionCount int  
set @QuestionCount=(select Count(id) from QuestionsDiscussed where interviewid=@InterviewMeetingId)  
if(@QuestionCount<5)  
begin  
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=16)  
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,16,@Point,@CreateDate)  
end  
if(@QuestionCount>5)  
begin  
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=17)  
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,17,@Point,@CreateDate)  
end  
--Selection Rate  
declare @CompletedInterviews int  
 set @CompletedInterviews=(Select Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Interviewer   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8))  
declare @SelectedInterview int  
set @SelectedInterview=(select Count(im.id) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Interviewer   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=2 or InterviewStatus=4))  
  
if((@SelectedInterview*100/@CompletedInterviews)<20 or (@SelectedInterview*100/@CompletedInterviews)>40)  
begin  
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=14)  
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,14,@Point,@CreateDate)  
end  
else if((@SelectedInterview*100/@CompletedInterviews)>=20 and (@SelectedInterview*100/@CompletedInterviews)<=40)  
begin  
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=15)  
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,15,@Point,@CreateDate)  
end  
End


