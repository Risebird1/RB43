 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_undoSuspiciousInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_undoSuspiciousInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_undoSuspiciousInterviewer
@InterviewerProfileId int
AS
BEGIN
update SuspiciousInterviewer set SuspiciousStatus=1 where UserId=@InterviewerProfileId
END
    

 