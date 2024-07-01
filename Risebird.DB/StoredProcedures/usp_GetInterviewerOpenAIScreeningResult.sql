IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerOpenAIScreeningResult'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerOpenAIScreeningResult
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetInterviewerOpenAIScreeningResult]     
@irfId int,
@userId int
As
Begin

 select top 1 OpenAIScreeningResponse, ResumeLink, ul.EmailId from TransInterviewer ti
 join InterviewerProfile ipr on ipr.id = ti.InterviewerId
 join UserList ul on ul.id = ipr.UserId
 where irfid = @irfId and ti.UserId = @userId

End
GO