-- Advanced Resume Screening: one result row per candidate per IRF.
--
-- Candidate is already one row per candidate per IRF, so CandidateId is unique here. The row is
-- also the work queue: Screen All / Re-screen set it to Pending and AdvancedResumeScreeningJob
-- claims Pending rows atomically (usp_GetCandidatesForAdvancedScreening).
--
-- Totals, verdict and authenticity rating are computed in code (AdvancedScreeningScorer), never
-- taken from the model. Candidate.AIScreeningResultJsonV2 is untouched by this feature.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'CandidateAdvancedScreening')
BEGIN
CREATE TABLE [dbo].[CandidateAdvancedScreening](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[CandidateId] [int] NOT NULL,
	[IRFId] [int] NOT NULL,
	[RubricVersion] [int] NULL,
	[InstructionsVersion] [int] NULL,
	-- Pending, Processing, Completed, Failed or Skipped.
	[Status] [varchar](20) NOT NULL CONSTRAINT [DF_CandidateAdvancedScreening_Status] DEFAULT ('Pending'),
	[TotalScore] [decimal](6, 2) NULL,
	[OverallPercentage] [decimal](5, 2) NULL,
	[Verdict] [varchar](30) NULL,
	[SkillsScore] [decimal](6, 2) NULL,
	[WorkScore] [decimal](6, 2) NULL,
	[EducationScore] [decimal](6, 2) NULL,
	[AuthenticityRating] [varchar](20) NULL,
	[AuthenticityFlagCount] [int] NULL,
	[ResponseJson] [nvarchar](max) NULL,
	[FactsJson] [nvarchar](max) NULL,
	[AttemptCount] [int] NOT NULL CONSTRAINT [DF_CandidateAdvancedScreening_AttemptCount] DEFAULT (0),
	[ErrorMessage] [nvarchar](1000) NULL,
	[ScreenedOn] [datetime] NULL,
	[TriggeredBy] [varchar](200) NULL,
	-- When the row last moved to Processing; a row stuck there for 30 minutes is reclaimed.
	[ClaimedOn] [datetime] NULL,
	[QueuedOn] [datetime] NULL,
 CONSTRAINT [PK_CandidateAdvancedScreening] PRIMARY KEY CLUSTERED ([id] ASC),
 CONSTRAINT [UQ_CandidateAdvancedScreening_CandidateId] UNIQUE ([CandidateId]),
 CONSTRAINT [FK_CandidateAdvancedScreening_Candidate] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidate] ([id])
) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_CandidateAdvancedScreening_IRFId_Status]
    ON [dbo].[CandidateAdvancedScreening] ([IRFId], [Status]);
END
GO
