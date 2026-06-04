-- Migration: Add Source and AutoClose tracking columns to InterviewerEmailAvailability

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Source' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [Source] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IsAutoClosed' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [IsAutoClosed] BIT NULL DEFAULT 0
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AutoClosedBy' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [AutoClosedBy] VARCHAR(100) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AutoClosedAt' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [AutoClosedAt] DATETIME NULL
END
GO
