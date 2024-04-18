IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCodeSnippetFile'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCodeSnippetFile
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetCodeSnippetFile
@interviewid int 
AS
BEGIN
Select Isnull(CodeSnippet,'') as CodeSnippet From Interviewmeetings where id=@interviewid
END
GO