IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'QcInterviewAIevaluation')
BEGIN  

CREATE TABLE [dbo].[QcInterviewAIevaluation](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NULL,
	[ReportAI] [bit] NULL,
	[TranscritAi] [bit] NULL,
	[PenalityApplied] [int] NULL,
	[QCDone] [bit] NULL,
	[QCDoneOn] [datetime] NULL,
	[ReportAIDetails] [varchar](max) NULL,
	[TranscritAiDetails] [varchar](max) NULL,
	[MergedAIQCDetails] [varchar](max) NULL,
	[AIMergeTotalPenalty] [int] NULL,
	[AIMergeInterviewerSuspicious] [varchar](50) NULL,
	[AIMergeRedoInterview] [bit] NULL,
	[ContradictionJson] [varchar](max) NULL,
	[ContradictionScore] [varchar](10) NULL,
	[ContradictionVerdict] [varchar](100) NULL,
	[ContradictionDirectionVerdict] [varchar](100) NULL,
	[qcmergeverdict] [varchar](100) NULL,
	[qcmergescore] [int] NULL,
 CONSTRAINT [PK_QcInterviewAIevaluation] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QcInterviewAIevaluation' AND COLUMN_NAME = 'ContradictionVerdict')
BEGIN
    ALTER TABLE [dbo].[QcInterviewAIevaluation] ADD [ContradictionVerdict] [varchar](100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QcInterviewAIevaluation' AND COLUMN_NAME = 'ContradictionDirectionVerdict')
BEGIN
    ALTER TABLE [dbo].[QcInterviewAIevaluation] ADD [ContradictionDirectionVerdict] [varchar](100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QcInterviewAIevaluation' AND COLUMN_NAME = 'qcmergeverdict')
BEGIN
    ALTER TABLE [dbo].[QcInterviewAIevaluation] ADD [qcmergeverdict] [varchar](100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QcInterviewAIevaluation' AND COLUMN_NAME = 'qcmergescore')
BEGIN
    ALTER TABLE [dbo].[QcInterviewAIevaluation] ADD [qcmergescore] [int] NULL;
END
GO

