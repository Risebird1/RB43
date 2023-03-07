 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveCapturedJoinMeetingTime'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCapturedJoinMeetingTime
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_SaveCapturedJoinMeetingTime
@interviewid int,
@emailid varchar(200),
@JoiningDate datetime
AS
BEGIN
Insert into InterviewMeetingsJoinTime(interviewid,interviewer,Joinedon)values(@interviewid,@emailid,@JoiningDate)
END
GO