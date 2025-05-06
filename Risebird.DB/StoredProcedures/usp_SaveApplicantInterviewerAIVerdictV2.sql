IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveApplicantInterviewerAIVerdictV2'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveApplicantInterviewerAIVerdictV2
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveApplicantInterviewerAIVerdictV2]     
@id int,
@completeAnalysis varchar(max),
@verdict varchar(50),
@score int,
@PerformedBy varchar(200),
@timeNow datetime
As
Begin

if(@completeAnalysis is not null and @completeAnalysis != 'null')
Begin
		

	if exists (select top 1 id from InterviewerDirectApplication where id=@id)
	Begin

		update InterviewerDirectApplication set OpenAIResponse = @completeAnalysis, AIVerdict = @verdict, AIAnalysisScore = @score, AIEvaluationPerformedBy = @PerformedBy, AIEvaluationGeneratedAt = @timeNow where id=@id

	End
		
	  
End

End
GO