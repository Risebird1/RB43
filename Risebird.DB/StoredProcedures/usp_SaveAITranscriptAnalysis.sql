IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveAITranscriptAnalysis'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveAITranscriptAnalysis
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveAITranscriptAnalysis]     
@interviewId int,
@aiAnalysisResponseJson varchar(max),
@verdict varchar(20),
@score int
As
Begin

if(@aiAnalysisResponseJson is not null and @aiAnalysisResponseJson != 'null' )
Begin
	  update InterviewComments set AITranscriptAnalysis = @aiAnalysisResponseJson, 
	  AITranscriptAnalysisScore = @score,
	  AITranscriptVerdict = @verdict
	  where InterviewId = @interviewId


End

End
GO
