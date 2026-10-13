-- Advanced Resume Screening: MinHash fingerprints of resume project / summary text, for finding
-- the same paragraph in other candidates' resumes (the ProjectDuplicate check).
--
-- MinHash is a 128 x int32 signature (512 bytes). ResumeFingerprintBand holds its locality-
-- sensitive-hash bands (32 bands of 4 values): two texts that share any band are candidate
-- matches, and their similarity is then estimated in code from the full signatures. That keeps
-- the duplicate search an index seek rather than a comparison against every resume.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'ResumeFingerprint')
BEGIN
CREATE TABLE [dbo].[ResumeFingerprint](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[CandidateId] [int] NOT NULL,
	[CandidateMasterId] [int] NULL,
	-- 'project' or 'summary'.
	[SectionType] [varchar](20) NOT NULL,
	[SectionName] [nvarchar](300) NULL,
	[MinHash] [varbinary](512) NOT NULL,
	[CreatedOn] [datetime] NULL,
 CONSTRAINT [PK_ResumeFingerprint] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_ResumeFingerprint_CandidateId] ON [dbo].[ResumeFingerprint] ([CandidateId]);
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'ResumeFingerprintBand')
BEGIN
CREATE TABLE [dbo].[ResumeFingerprintBand](
	[FingerprintId] [int] NOT NULL,
	[BandNo] [tinyint] NOT NULL,
	[BandHash] [bigint] NOT NULL,
 CONSTRAINT [PK_ResumeFingerprintBand] PRIMARY KEY CLUSTERED ([FingerprintId] ASC, [BandNo] ASC),
 CONSTRAINT [FK_ResumeFingerprintBand_Fingerprint] FOREIGN KEY ([FingerprintId])
    REFERENCES [dbo].[ResumeFingerprint] ([id]) ON DELETE CASCADE
) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_ResumeFingerprintBand_BandHash]
    ON [dbo].[ResumeFingerprintBand] ([BandNo], [BandHash]) INCLUDE ([FingerprintId]);
END
GO
