IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveResumeFingerprint'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveResumeFingerprint
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: stores one MinHash fingerprint (project or summary text) and its
-- LSH bands. Call usp_DeleteResumeFingerprints for the candidate first when re-fingerprinting.
Create PROCEDURE [dbo].[usp_SaveResumeFingerprint]
@CandidateId int,
@CandidateMasterId int = null,
@SectionType varchar(20),
@SectionName nvarchar(300) = null,
@MinHash varbinary(512),
@Bands dbo.ResumeFingerprintBand_TableType READONLY
AS
Begin
	SET NOCOUNT ON;

	declare @id int

	insert into ResumeFingerprint (CandidateId, CandidateMasterId, SectionType, SectionName, MinHash, CreatedOn)
	values (@CandidateId, @CandidateMasterId, @SectionType, @SectionName, @MinHash, GETUTCDATE())

	set @id = SCOPE_IDENTITY()

	insert into ResumeFingerprintBand (FingerprintId, BandNo, BandHash)
	select @id, BandNo, BandHash from @Bands

	select @id as FingerprintId
End
GO

