IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveCandidateWorkHistory'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCandidateWorkHistory
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: upserts the work history extracted from one candidate's resume.
Create PROCEDURE [dbo].[usp_SaveCandidateWorkHistory]
@CandidateId int,
@CandidateMasterId int,
@IRFId int = null,
@HistoryJson nvarchar(max)
AS
Begin
	SET NOCOUNT ON;

	if exists (select 1 from CandidateWorkHistory where CandidateId = @CandidateId)
		update CandidateWorkHistory set CandidateMasterId = @CandidateMasterId, IRFId = @IRFId,
			HistoryJson = @HistoryJson, ExtractedOn = GETUTCDATE()
		where CandidateId = @CandidateId
	else
		insert into CandidateWorkHistory (CandidateId, CandidateMasterId, IRFId, HistoryJson, ExtractedOn)
		values (@CandidateId, @CandidateMasterId, @IRFId, @HistoryJson, GETUTCDATE())
End
GO
