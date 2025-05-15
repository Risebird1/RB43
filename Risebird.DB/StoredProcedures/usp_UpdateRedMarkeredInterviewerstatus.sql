IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateRedMarkeredInterviewerstatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateRedMarkeredInterviewerstatus
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateRedMarkeredInterviewerstatus
@interviewid int,
@IRFid int,
@status int
AS
BEGIN
Update TransInterviewer set RedMarkerStatus=@status where IRFId=@IRFid and Userid=@interviewid
END
Go