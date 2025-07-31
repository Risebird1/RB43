
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveAIInterviewProctorEvaluation'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveAIInterviewProctorEvaluation
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveAIInterviewProctorEvaluation]     
@interviewId int,
@InterviewerComment varchar(max),
@MeetingStatus int  = null,
@InterviewStatus int  = null,
@TotalTechScore float = null
As
Begin

update InterviewComments set MeetingStatus = @MeetingStatus, 
	  InterviewStatus = @InterviewStatus,
	  InterviewerComment = @InterviewerComment,
	  TotalTechScore =@TotalTechScore
	  where InterviewId = @interviewId
if exists(select top 1 id from InterviewReport where InterviewId  = @interviewId)
Begin
 update InterviewReport set ModifiedOn = getdate() where InterviewId = @interviewId
End
Else
Begin
 insert into InterviewReport (InterviewId, IRFId, CreatedOn) values (@interviewId, (select id from IRF where IRFId = (select irfId from InterviewMeetings where id = @interviewId)), getdate())
End
End
GO
