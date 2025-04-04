IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerOpenAIScreeningResultsV2'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerOpenAIScreeningResultsV2
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveInterviewerOpenAIScreeningResultsV2]     
@irfId int,
@userId int,
@completeFitmentAnalysis varchar(max),
@fitmentScore int,
@verdict varchar(25)
As
Begin

if(@completeFitmentAnalysis is not null and @completeFitmentAnalysis != 'null')
Begin
	  update TransInterviewer set OpenAIScreeningResponse = @completeFitmentAnalysis,
	  AIVerdict=@verdict, 
	  AIScreeningScore = @fitmentScore 
	  where irfid = @irfId and UserId = @userId

End

End
GO

