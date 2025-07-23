IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateTimeLeft'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateTimeLeft
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateTimeLeft
@SessionId varchar(200),
@interviewId int,
@secondsLeft int,
@UpdatedAt datetime
AS
BEGIN

update InterviewMeetings set TimeLeftInSeconds =@secondsLeft where id = @interviewId

END
GO