IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_addRedoInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_addRedoInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].usp_addRedoInterview     
@InterviewId int,
@Type varchar(100)
as
begin
Declare @CreadtedBy varchar(200)
Select @CreadtedBy=candidateEmail From InterviewMeetings where id=@InterviewId
IF EXISTS(select * from RedoInterviews where interviewid=@interviewid)
begin
Update RedoInterviews set ModifiedOn=GETDATE(),ModifiedBy=@CreadtedBy where interviewid=@interviewid
end
else
begin
Insert Into RedoInterviews(interviewid,Type,Approved,Createdon,CreatedBy)values(@InterviewId,@Type,0,Getdate(),@CreadtedBy)
end
End
Go