IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveCandidateAdvancedResumeScreeningResults'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCandidateAdvancedResumeScreeningResults
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Saves an advanced (rubric-based) resume screening. The verdict and fitment percentage go to
-- the same two columns every candidate list and report already reads, so those show the latest
-- screening without change; the full analysis goes to its own column.
Create PROCEDURE [dbo].[usp_SaveCandidateAdvancedResumeScreeningResults]
@irfId int,
@candidateMasterId int,
@verdict varchar(30),
@resumeScreeningScore int,
@screeningResponseJson varchar(max)
As
Begin

if(@screeningResponseJson is not null and @screeningResponseJson != 'null')
Begin
	 update Candidate set AIAdvancedScreeningResultJson = @screeningResponseJson, AIResumeScreeningVerdict = @verdict,
	 AIResumeScreeningScore = @resumeScreeningScore
	 where irfid = @irfId and CandidateMasterId = @candidateMasterId
End

End
GO
