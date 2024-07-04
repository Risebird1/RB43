IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveTransInterviewerOpenAIScreeningResults'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveTransInterviewerOpenAIScreeningResults
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveTransInterviewerOpenAIScreeningResults]     
@transInterviewerId int,
@screeningResponseJson varchar(max)
As
Begin

  update TransInterviewer set OpenAIScreeningResponse = @screeningResponseJson where id = @transInterviewerId

End
GO