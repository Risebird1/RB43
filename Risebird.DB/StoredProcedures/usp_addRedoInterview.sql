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
Insert Into RedoInterviews(interviewid,Type,Approved)values(@InterviewId,@Type,0)
End
Go