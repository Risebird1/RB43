IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FindSimilarResumeFingerprints'))
BEGIN
    DROP PROCEDURE dbo.usp_FindSimilarResumeFingerprints
END
GO

-- Candidate duplicate matches for one text: other people's fingerprints sharing at least one LSH
-- band with @Bands. Same-person fingerprints (same CandidateMasterId) are excluded, because a
-- person reusing their own project text across applications is not a duplicate. The similarity
-- itself is estimated in code from the returned signatures.
Create PROCEDURE [dbo].[usp_FindSimilarResumeFingerprints]
@CandidateId int,
@CandidateMasterId int = null,
@Bands dbo.ResumeFingerprintBand_TableType READONLY
AS
Begin
	SET NOCOUNT ON;

	select top 200 f.id as FingerprintId, f.CandidateId, f.CandidateMasterId, f.SectionType, f.SectionName, f.MinHash
	from ResumeFingerprint f
	where f.id in (
		select distinct b.FingerprintId
		from ResumeFingerprintBand b
		join @Bands q on q.BandNo = b.BandNo and q.BandHash = b.BandHash
	)
	and f.CandidateId <> @CandidateId
	and (@CandidateMasterId is null or f.CandidateMasterId is null or f.CandidateMasterId <> @CandidateMasterId)
End
GO
