IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateWorkHistories'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateWorkHistories
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: the same person's work histories extracted from their other
-- applications (other IRFs), newest first - for the "consistency across versions" check.
Create PROCEDURE [dbo].[usp_GetCandidateWorkHistories]
@CandidateMasterId int,
@ExcludeCandidateId int
AS
Begin
	SET NOCOUNT ON;

	select top 5 CandidateId, CandidateMasterId, IRFId, HistoryJson, ExtractedOn
	from CandidateWorkHistory
	where CandidateMasterId = @CandidateMasterId and CandidateId <> @ExcludeCandidateId
	and HistoryJson is not null
	order by ExtractedOn desc
End
GO
