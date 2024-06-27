IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerOpenAIScreeningResults'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerOpenAIScreeningResults
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveInterviewerOpenAIScreeningResults]     
@irfId int,
@interviewerProfileId int,
@screeningResponseJson varchar(max)
As
Begin

  update TransInterviewer set OpenAIScreeningResponse = @screeningResponseJson where irfid = @irfId and InterviewerId = @interviewerProfileId

End
GO