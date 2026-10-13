-- The LSH band hashes of one ResumeFingerprint, saved in one call by usp_SaveResumeFingerprint.
-- SQL Server cannot alter a table type in place: if this shape must change, add a new
-- versioned type instead of editing this one.
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'ResumeFingerprintBand_TableType')
BEGIN
CREATE TYPE [dbo].[ResumeFingerprintBand_TableType] AS TABLE(
	[BandNo] [tinyint] NOT NULL,
	[BandHash] [bigint] NOT NULL
)
END
GO
