-- Stores one copy of an AI code interview report per source:
-- ReportSource 0 = the report produced by the AI assistant, 1 = the expert's edited copy.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'AICodeInterviewReport')
BEGIN
CREATE TABLE [dbo].[AICodeInterviewReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NOT NULL,
	[InterviewId] [int] NULL,
	[ReportSource] [tinyint] NOT NULL,
	[OverallVerdict] [nvarchar](200) NULL,
	[OverallComment] [nvarchar](max) NULL,
	[OverallCommunicationRating] [decimal](5,2) NULL,
	[OverallCommunicationComment] [nvarchar](max) NULL,
	[RubricRatingsJson] [nvarchar](max) NULL,
	[CreatedAt] [datetime] NULL,
	[CreatedBy] [varchar](150) NULL,
	[ModifiedAt] [datetime] NULL,
	[ModifiedBy] [varchar](150) NULL,
 CONSTRAINT [PK_AICodeInterviewReport] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

-- One report per source per session, so resubmission replaces the expert copy in place
-- rather than accumulating duplicates.
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'UQ_AICodeInterviewReport_SessionSource'
    AND object_id = OBJECT_ID('AICodeInterviewReport')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AICodeInterviewReport_SessionSource
    ON AICodeInterviewReport (SessionId, ReportSource);
END
GO
