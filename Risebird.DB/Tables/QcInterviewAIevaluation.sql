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
 CONSTRAINT [PK_QcInterviewAIevaluation] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO


