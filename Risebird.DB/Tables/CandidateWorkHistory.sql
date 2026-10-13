-- Advanced Resume Screening: the employers, titles, dates and projects extracted from a
-- candidate's resume. Used for the stability facts and for comparing a person's histories
-- across IRFs (rows share CandidateMasterId). One row per Candidate, i.e. per IRF application.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'CandidateWorkHistory')
BEGIN
CREATE TABLE [dbo].[CandidateWorkHistory](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[CandidateId] [int] NOT NULL,
	[CandidateMasterId] [int] NOT NULL,
	[IRFId] [int] NULL,
	[HistoryJson] [nvarchar](max) NULL,
	[ExtractedOn] [datetime] NULL,
 CONSTRAINT [PK_CandidateWorkHistory] PRIMARY KEY CLUSTERED ([id] ASC),
 CONSTRAINT [UQ_CandidateWorkHistory_CandidateId] UNIQUE ([CandidateId])
) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_CandidateWorkHistory_CandidateMasterId]
    ON [dbo].[CandidateWorkHistory] ([CandidateMasterId]);
END
GO
