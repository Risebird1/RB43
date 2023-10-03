IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ApproveRedoInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_ApproveRedoInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_ApproveRedoInterviews
@InterviewId int,
@ModifyBy varchar(100),
@ModifyDate DateTime
AS
BEGIN 
UPDATE RedoInterviews set Approved =1,ModifiedBy=@ModifyBy,ModifiedOn=@ModifyDate where interviewid=@InterviewId
UPDATE InterviewMeetings set RedoInterview=1,sModifiedBy=@ModifyBy,dtModifiedOn=@ModifyDate where id=@InterviewId
END
GO