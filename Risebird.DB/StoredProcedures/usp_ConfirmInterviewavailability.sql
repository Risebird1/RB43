IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ConfirmInterviewavailability'))
BEGIN
    DROP PROCEDURE dbo.usp_ConfirmInterviewavailability
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_ConfirmInterviewavailability
@emailId varchar(150),
@Meetingid int,
@CreatedOn datetime,
@Action varchar(10)
AS
BEGIN
Select * From Interviewclearness
Insert into InterviewClearness(Interviewer,CreatedOn,Interviewid,Action) values(@emailId,@CreatedOn,@Meetingid,@Action)
END
GO