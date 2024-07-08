IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveCandidateOpenAIScreeningResults'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCandidateOpenAIScreeningResults
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveCandidateOpenAIScreeningResults]     
@irfId int,
@candidateMasterId int,
@screeningResponseJson varchar(max)
As
Begin

if(@screeningResponseJson is not null and @screeningResponseJson != 'null')
Begin
	 update Candidate set OpenAIScreeningResponseJson = @screeningResponseJson where irfid = @irfId and CandidateMasterId = @candidateMasterId
End

End
GO