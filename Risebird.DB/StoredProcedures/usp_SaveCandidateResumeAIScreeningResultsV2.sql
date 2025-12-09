IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveCandidateResumeAIScreeningResultsV2'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCandidateResumeAIScreeningResultsV2
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveCandidateResumeAIScreeningResultsV2]     -- V1 : usp_SaveCandidateOpenAIScreeningResults
@irfId int,
@candidateMasterId int,
@verdict varchar(30),
@screeningResponseJson varchar(max)
As
Begin

if(@screeningResponseJson is not null and @screeningResponseJson != 'null')
Begin
	 update Candidate set AIScreeningResultJsonV2 = @screeningResponseJson, AIResumeScreeningVerdict = @verdict where irfid = @irfId and CandidateMasterId = @candidateMasterId
End

End
GO