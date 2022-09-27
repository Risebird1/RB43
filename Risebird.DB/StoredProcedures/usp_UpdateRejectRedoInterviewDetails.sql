IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateRejectRedoInterviewDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateRejectRedoInterviewDetails
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateRejectRedoInterviewDetails  
@interviewid int,  
@id int,  
@RejectionComment varchar(500),  
@ModifiedBy varchar(200)  
AS  
BEGIN  
Update RedoInterviews Set RejectionComment=@RejectionComment,ModifiedOn=Getdate(),ModifiedBy=@ModifiedBy where id=@id  
Update InterviewMeetings set RedoInterview=0 where id=@interviewid  
END
Go