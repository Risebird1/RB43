-- Table to store listening comprehension assessment results
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ListeningComprehensionAssessment')
BEGIN  
CREATE TABLE [dbo].[ListeningComprehensionAssessment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NULL,
	[QuestionId] [int] NULL,
	[AudioUrl] [varchar](max) NULL,
	[AudioTranscription] [nvarchar](max) NULL,
	[CandidateSummary] [nvarchar](max) NULL,
	[ExpectedAnswer] [nvarchar](max) NULL,
	[TimeStarted] [datetime] NULL,
	[TimeCompleted] [datetime] NULL,
	[TimeTakenInSeconds] [decimal](10,2) NULL,
	[AudioPlayCount] [int] NULL,
	[IsTimeExpired] [bit] NULL,
	[Score] [decimal](5,2) NULL,
	[MaxScore] [decimal](5,2) NULL,
	[IsPassed] [bit] NULL,
	[OpenAIAnalysisJson] [nvarchar](max) NULL,
	[OpenAIResponseId] [varchar](100) NULL,
	[CreatedAt] [datetime] NULL,
	[CreatedBy] [varchar](150) NULL,
 CONSTRAINT [PK_ListeningComprehensionAssessment] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
ELSE
BEGIN

-- Expected answer snapshotted at submission time, so re-scoring stays reproducible
-- even if the question's expected answer is later edited.
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ExpectedAnswer')
      AND Object_ID = Object_ID(N'ListeningComprehensionAssessment'))
BEGIN
    ALTER TABLE ListeningComprehensionAssessment
    Add ExpectedAnswer nvarchar(max) NULL
END

-- How many times the candidate actually played the audio (max 2). Anti-cheat signal.
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AudioPlayCount')
      AND Object_ID = Object_ID(N'ListeningComprehensionAssessment'))
BEGIN
    ALTER TABLE ListeningComprehensionAssessment
    Add AudioPlayCount int NULL
END

-- Set when the three-minute allowance ran out and the answer was auto-submitted.
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsTimeExpired')
      AND Object_ID = Object_ID(N'ListeningComprehensionAssessment'))
BEGIN
    ALTER TABLE ListeningComprehensionAssessment
    Add IsTimeExpired bit NULL DEFAULT 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Score')
      AND Object_ID = Object_ID(N'ListeningComprehensionAssessment'))
BEGIN
    ALTER TABLE ListeningComprehensionAssessment
    Add Score decimal(5,2) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'MaxScore')
      AND Object_ID = Object_ID(N'ListeningComprehensionAssessment'))
BEGIN
    ALTER TABLE ListeningComprehensionAssessment
    Add MaxScore decimal(5,2) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsPassed')
      AND Object_ID = Object_ID(N'ListeningComprehensionAssessment'))
BEGIN
    ALTER TABLE ListeningComprehensionAssessment
    Add IsPassed bit NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'OpenAIAnalysisJson')
      AND Object_ID = Object_ID(N'ListeningComprehensionAssessment'))
BEGIN
    ALTER TABLE ListeningComprehensionAssessment
    Add OpenAIAnalysisJson nvarchar(max) NULL
END

-- Responses API response id, kept for traceability of the score.
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'OpenAIResponseId')
      AND Object_ID = Object_ID(N'ListeningComprehensionAssessment'))
BEGIN
    ALTER TABLE ListeningComprehensionAssessment
    Add OpenAIResponseId varchar(100) NULL
END

END
GO
