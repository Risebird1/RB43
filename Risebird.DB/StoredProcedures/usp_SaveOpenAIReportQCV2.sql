IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveOpenAIReportQCV2'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveOpenAIReportQCV2
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveOpenAIReportQCV2]     
@interviewId int,
@completeAnalysis varchar(max),
@score int,
@verdict varchar(25)
As
Begin

if(@completeAnalysis is not null and @completeAnalysis != 'null')
Begin
	  update InterviewComments set OpenAIQCResponse = @completeAnalysis,
	  AIVerdict=@verdict, 
	  AIReportQCAnalysisScore = @score 
	  where interviewId =@interviewId

End

End
GO

