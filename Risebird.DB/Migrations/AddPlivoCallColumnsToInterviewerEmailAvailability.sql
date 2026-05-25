-- Migration: Add Plivo voice agent call tracking columns to InterviewerEmailAvailability
-- These columns track the outbound availability call made via Plivo CX to confirm interviewer availability.

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallStatus' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoCallStatus] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoConversationId' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoConversationId] VARCHAR(200) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallAt' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoCallAt] DATETIME NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallDuration' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoCallDuration] INT NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallTranscript' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoCallTranscript] NVARCHAR(MAX) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallSummary' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoCallSummary] NVARCHAR(MAX) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoIntent' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoIntent] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoAssignmentHelp' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoAssignmentHelp] BIT NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoEmailReceived' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoEmailReceived] BIT NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallbackRequested' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoCallbackRequested] BIT NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoNotes' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoNotes] NVARCHAR(500) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCalledBy' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [PlivoCalledBy] VARCHAR(150) NULL
END
GO
