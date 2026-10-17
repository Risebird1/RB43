IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SuspiciousInterviewer')
BEGIN
CREATE TABLE [dbo].[SuspiciousInterviewer](
	[Id] [int] IDENTITY(1,101) NOT NULL,
	[UserId] [int] NULL,
	[Comment] [varchar](500) NULL,
	[Createddate] [datetime] NULL,
	[CreatedBy] [varchar](200) NULL,
	[SuspiciousStatus] [bit] NULL,
	[ModifyBy] [varchar](100) NULL,
	[ModifyDate] [datetime] NULL,
 CONSTRAINT [PK_InterviewSuspicious] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifyBy')
      AND Object_ID = Object_ID(N'SuspiciousInterviewer'))
BEGIN
    ALTER TABLE SuspiciousInterviewer
    Add ModifyBy varchar(100)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifyDate')
      AND Object_ID = Object_ID(N'SuspiciousInterviewer'))
BEGIN
    ALTER TABLE SuspiciousInterviewer
    Add ModifyDate datetime
END
-- QC Tracking Delta 1: the box allows 250 characters but Comment was varchar(100), so longer marks failed to
-- save. Widen Comment to 500 and CreatedBy to 200 on the existing table (runs only while still narrower).
IF COL_LENGTH(N'dbo.SuspiciousInterviewer', N'Comment') < 500
    ALTER TABLE dbo.SuspiciousInterviewer ALTER COLUMN Comment VARCHAR(500) NULL
IF COL_LENGTH(N'dbo.SuspiciousInterviewer', N'CreatedBy') < 200
    ALTER TABLE dbo.SuspiciousInterviewer ALTER COLUMN CreatedBy VARCHAR(200) NULL
-- Page the mark was made from (QC Activity)
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SourcePage')
      AND Object_ID = Object_ID(N'SuspiciousInterviewer'))
BEGIN
    ALTER TABLE SuspiciousInterviewer
    Add SourcePage varchar(60) NULL
END
GO


