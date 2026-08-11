-- Table to store coding question assessment results
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CodingAssessment')
BEGIN  
CREATE TABLE [dbo].[CodingAssessment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NULL,
	[QuestionId] [int] NULL,
	[QuestionText] [nvarchar](max) NULL,
	[CodeLanguage] [varchar](50) NULL,
	[SubmittedCode] [nvarchar](max) NULL,
	[TimeLimitInMinutes] [int] NULL,
	[TimeStarted] [datetime] NULL,
	[TimeCompleted] [datetime] NULL,
	[TimeTakenInSeconds] [decimal](10,2) NULL,
	[IsTimeExpired] [bit] NULL,
	[CreatedAt] [datetime] NULL,
	[CreatedBy] [varchar](150) NULL,
 CONSTRAINT [PK_CodingAssessment] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

-- =====================================================
-- Add unique index to prevent duplicate entries
-- =====================================================
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_CodingAssessment_SessionQuestion' 
    AND object_id = OBJECT_ID('CodingAssessment')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_CodingAssessment_SessionQuestion
    ON CodingAssessment (SessionId, QuestionId);
END
GO
