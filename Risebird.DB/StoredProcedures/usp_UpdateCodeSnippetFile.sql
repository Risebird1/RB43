IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateCodeSnippetFile'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCodeSnippetFile
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateCodeSnippetFile
@interviewid int,
@uploadedFileURL varchar(1000),
@CreatedOn datetime
AS
BEGIN
Update InterviewMeetings set CodeSnippet=@uploadedFileURL where id=@interviewid
END
GO