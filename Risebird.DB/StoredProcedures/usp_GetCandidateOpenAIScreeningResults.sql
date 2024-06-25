IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateOpenAIScreeningResults'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateOpenAIScreeningResults
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetCandidateOpenAIScreeningResults]     
@irfId int,
@candidateMasterId int
As
Begin
 select top 1 OpenAIScreeningResponseJson, ResumeLink from Candidate where irfid = @irfId and CandidateMasterId = @candidateMasterId
End
GO