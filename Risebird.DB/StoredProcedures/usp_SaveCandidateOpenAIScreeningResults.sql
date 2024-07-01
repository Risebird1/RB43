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

 update Candidate set OpenAIScreeningResponseJson = @screeningResponseJson where irfid = @irfId and CandidateMasterId = @candidateMasterId

End
GO