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
@status int,
@Type varchar(10),
@Comment varchar(300),
@CreatedBy varchar(200)
AS
BEGIN
Update TransInterviewer set RedMarkerStatus=@status where IRFId=@IRFid and Userid=@interviewid
--Insert into Redmarker Comments With History
Insert into RedMarkerStatusDetails(InterviewerUserid,IRFId,Comment,Type,CreatedOn,CreatedBy,Status) 
values(@interviewid,@IRFid,@Comment,@Type,DateAdd(Minute,330,GETDATE()),@CreatedBy,@status)
END
Go