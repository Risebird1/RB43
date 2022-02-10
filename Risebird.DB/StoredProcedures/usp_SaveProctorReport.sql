IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveProctorReport'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveProctorReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_SaveProctorReport
@interviewid int,
@ProctorReport varchar(1000)
AS
BEGIN
Update InterviewMeetings set ProctorReport=@ProctorReport where id=@interviewid
END
GO