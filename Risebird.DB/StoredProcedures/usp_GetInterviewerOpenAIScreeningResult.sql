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
@interviewerProfileId int
As
Begin

 select top 1 OpenAIScreeningResponse, ResumeLink from TransInterviewer ti
 join InterviewerProfile ipr on ipr.id = ti.InterviewerId
 where irfid = @irfId and InterviewerId = @interviewerProfileId

End
GO