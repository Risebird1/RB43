
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_TransInterviewersReliabilityScores'))
BEGIN
    DROP PROCEDURE dbo.usp_TransInterviewersReliabilityScores
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_TransInterviewersReliabilityScores
@InterviewMeetingId int,
@ReliabilityScoreTypeId int,
@CreateDate datetime,
@InterviewerEmail varchar(100)
as
begin
declare @Point int
if(@InterviewMeetingId>0)
begin
declare @InterviewerId int
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=@ReliabilityScoreTypeId)
set @InterviewerId=(select ip.Id from InterviewerProfile ip join UserList ul on ip.UserId=ul.Id join InterviewMeetings im on ul.EmailId=im.interviewer where Im.id= @InterviewMeetingId)
If Exists(Select * from TransInterviewersReliabilityScores where InterviewerId=@InterviewerId AND InterviewMeetingId=@InterviewMeetingId AND ReliabilityScoreTypeId=@ReliabilityScoreTypeId)
Begin
Update TransInterviewersReliabilityScores set InterviewMeetingId=@InterviewMeetingId,ReliabilityScoreTypeId=@ReliabilityScoreTypeId,Point=@Point,CreatedDate=@CreateDate where InterviewerId=@InterviewerId
End
Else
Begin
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,@InterviewMeetingId,@ReliabilityScoreTypeId,@Point,@CreateDate)
end
end
else
begin
set @Point=(Select case when PositiveScore>0 OR PositiveScore<0 then PositiveScore when NegativeScore>0 OR NegativeScore<0 then NegativeScore end as Point from ReliabilityScoreTypes where id=@ReliabilityScoreTypeId)
set @InterviewerId=(select ip.Id from InterviewerProfile ip join UserList ul on ip.UserId=ul.Id join InterviewMeetings im on ul.EmailId=im.interviewer where Im.interviewer=@InterviewerEmail)
Insert into TransInterviewersReliabilityScores(InterviewerId,InterviewMeetingId,ReliabilityScoreTypeId,Point,CreatedDate) values(@InterviewerId,null,@ReliabilityScoreTypeId,@Point,@CreateDate)
end
end
GO


