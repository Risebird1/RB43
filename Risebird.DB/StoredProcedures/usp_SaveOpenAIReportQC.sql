IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveOpenAIReportQC'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveOpenAIReportQC
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveOpenAIReportQC]     
@interviewId int,
@openAIQcResponseJson varchar(max)
As
Begin

if(@openAIQcResponseJson is not null and @openAIQcResponseJson != 'null' )
Begin
	  update InterviewComments set OpenAIQCResponse = @openAIQcResponseJson where InterviewId = @interviewId

End

End
GO