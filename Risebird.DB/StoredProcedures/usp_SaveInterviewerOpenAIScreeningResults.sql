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
@userId int,
@screeningResponseJson varchar(max)
As
Begin

if(@screeningResponseJson is not null and @screeningResponseJson != 'null')
Begin
	  update TransInterviewer set OpenAIScreeningResponse = @screeningResponseJson where irfid = @irfId and UserId = @userId
End

End
GO