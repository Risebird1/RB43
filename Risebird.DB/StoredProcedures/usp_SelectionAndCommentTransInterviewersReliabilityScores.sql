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
set @Interviewer=(select im.Interviewer from InterviewMeetings im join UserList ul on im.interviewer=ul.EmailId where im.id=@InterviewMeetingId)

set @InterviewerStatus=(Select InterviewStatus from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Interviewer 
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8))

set @InterviewerId=(select ip.Id from InterviewerProfile ip join UserList ul on ip.UserId=ul.Id join InterviewMeetings im on ul.EmailId=im.interviewer where Im.id= @InterviewMeetingId)
if(@InterviewerStatus=2 or @InterviewerStatus=4)
begin
declare @SelectedInterviewsLength int
set @SelectedInterviewsLength=(Select len(ic.InterviewerComment) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Interviewer 
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=2 or InterviewStatus=4))

if(@SelectedInterviewsLength<500)
begin 
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=20)
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,20,@Point,@CreateDate)
end
if(@SelectedInterviewsLength>500)
begin 
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=21)
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,21,@Point,@CreateDate)
end
end

if(@InterviewerStatus=3 or @InterviewerStatus=1)
begin
declare @RejectedInterviewsLength int
set @SelectedInterviewsLength=(Select len(ic.InterviewerComment) from Interviewmeetings im join InterviewComments ic on im.id=ic.InterviewId where confirmationStatus=1 and  interviewer=@Interviewer 
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and (InterviewStatus=3 or InterviewStatus=1))
if(@RejectedInterviewsLength<250)
begin
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=18)
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,18,@Point,@CreateDate)
end
if(@RejectedInterviewsLength>250)
begin
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=19)
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,19,@Point,@CreateDate)
end
end
End


