-- Interviewer report edit window: permanent audit trail of every interview report save.
-- One row per save of the report form (Action = 'Submit' on the first save, 'Edit' after it) and per
-- admin comments save on an interview that already has a submitted report (Action = 'CommentsEdit').
-- Written for every profile and every IRF, whether or not IRF.EnableReportEditWindow is on.
-- Additive and idempotent: safe to run more than once, and safe to leave in place on rollback.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'InterviewReportEditLog')
BEGIN
CREATE TABLE [dbo].[InterviewReportEditLog](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[IRFId] [int] NULL,
	[Action] [varchar](20) NOT NULL,
	[PerformedBy] [varchar](200) NULL,
	[PerformedOn] [datetime] NOT NULL,
	[OldInterviewStatus] [int] NULL,
	[NewInterviewStatus] [int] NULL,
	[OldMeetingStatus] [int] NULL,
	[NewMeetingStatus] [int] NULL,
	[OldInterviewerComment] [varchar](max) NULL,
	[NewInterviewerComment] [varchar](max) NULL,
	[OldOverallScore] [float] NULL,
	[NewOverallScore] [float] NULL,
	[EditReason] [nvarchar](2000) NULL,
	[Source] [varchar](50) NULL,
	FOREIGN KEY (InterviewId) REFERENCES InterviewMeetings(id),
 CONSTRAINT [PK_InterviewReportEditLog] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Columns added after the first version of the table, for databases that already have it.
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'EditReason' AND Object_ID = Object_ID(N'InterviewReportEditLog'))
BEGIN
    ALTER TABLE InterviewReportEditLog Add EditReason nvarchar(2000) NULL
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'OldOverallScore' AND Object_ID = Object_ID(N'InterviewReportEditLog'))
BEGIN
    ALTER TABLE InterviewReportEditLog Add OldOverallScore float NULL
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'NewOverallScore' AND Object_ID = Object_ID(N'InterviewReportEditLog'))
BEGIN
    ALTER TABLE InterviewReportEditLog Add NewOverallScore float NULL
END

-- Which form the save came from: 'AddReport', 'ProctorReport' or 'Comments'.
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Source' AND Object_ID = Object_ID(N'InterviewReportEditLog'))
BEGIN
    ALTER TABLE InterviewReportEditLog Add Source varchar(50) NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_InterviewReportEditLog_InterviewId' AND object_id = OBJECT_ID(N'InterviewReportEditLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewReportEditLog_InterviewId ON dbo.InterviewReportEditLog (InterviewId)
END
GO
