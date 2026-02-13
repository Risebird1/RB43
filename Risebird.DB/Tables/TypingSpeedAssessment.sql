-- Table to store typing speed assessment results
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TypingSpeedAssessment')
BEGIN  
CREATE TABLE [dbo].[TypingSpeedAssessment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NULL,
	[QuestionId] [int] NULL,
	[SampleText] [nvarchar](max) NULL,
	[TypedText] [nvarchar](max) NULL,
	[TimeStarted] [datetime] NULL,
	[TimeCompleted] [datetime] NULL,
	[TimeTakenInSeconds] [decimal](10,2) NULL,
	[TotalCharacters] [int] NULL,
	[CorrectCharacters] [int] NULL,
	[AccuracyPercentage] [decimal](5,2) NULL,
	[WordsPerMinute] [decimal](10,2) NULL,
	[PassingWPM] [decimal](10,2) NULL DEFAULT 30.00,
	[PassingAccuracyPercentage] [decimal](5,2) NULL DEFAULT 97.00,
	[IsPassed] [bit] NULL,
	[OpenAIAnalysisJson] [nvarchar](max) NULL,
	[OpenAIThreadId] [varchar](100) NULL,
	[OpenAIRunId] [varchar](100) NULL,
	[CreatedAt] [datetime] NULL,
	[CreatedBy] [varchar](150) NULL,
 CONSTRAINT [PK_TypingSpeedAssessment] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
ELSE
BEGIN
	-- Add new columns if they don't exist
	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'PassingWPM')
		  AND Object_ID = Object_ID(N'TypingSpeedAssessment'))
	BEGIN
		ALTER TABLE TypingSpeedAssessment
		Add PassingWPM decimal(10,2) NULL DEFAULT 30.00
	END

	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'PassingAccuracyPercentage')
		  AND Object_ID = Object_ID(N'TypingSpeedAssessment'))
	BEGIN
		ALTER TABLE TypingSpeedAssessment
		Add PassingAccuracyPercentage decimal(5,2) NULL DEFAULT 97.00
	END

	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'IsPassed')
		  AND Object_ID = Object_ID(N'TypingSpeedAssessment'))
	BEGIN
		ALTER TABLE TypingSpeedAssessment
		Add IsPassed bit NULL
	END

	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'OpenAIAnalysisJson')
		  AND Object_ID = Object_ID(N'TypingSpeedAssessment'))
	BEGIN
		ALTER TABLE TypingSpeedAssessment
		Add OpenAIAnalysisJson nvarchar(max) NULL
	END

	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'OpenAIThreadId')
		  AND Object_ID = Object_ID(N'TypingSpeedAssessment'))
	BEGIN
		ALTER TABLE TypingSpeedAssessment
		Add OpenAIThreadId varchar(100) NULL
	END

	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'OpenAIRunId')
		  AND Object_ID = Object_ID(N'TypingSpeedAssessment'))
	BEGIN
		ALTER TABLE TypingSpeedAssessment
		Add OpenAIRunId varchar(100) NULL
	END
END
GO

-- =====================================================
-- Add unique index to prevent duplicate entries
-- =====================================================
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_TypingSpeedAssessment_SessionQuestion' 
    AND object_id = OBJECT_ID('TypingSpeedAssessment')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_TypingSpeedAssessment_SessionQuestion
    ON TypingSpeedAssessment (SessionId, QuestionId);
    PRINT '✓ Unique constraint added to TypingSpeedAssessment to prevent duplicates';
END
GO
