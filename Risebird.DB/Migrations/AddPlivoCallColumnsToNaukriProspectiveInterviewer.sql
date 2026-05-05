-- Migration: Add Plivo voice agent call tracking columns to NaukriProspectiveInterviewer
-- These columns track the outbound recruitment call made via Plivo CX to gauge interviewer interest.

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallStatus' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoCallStatus] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoConversationId' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoConversationId] NVARCHAR(255) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallAt' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoCallAt] DATETIME NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallDuration' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoCallDuration] INT NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallTranscript' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoCallTranscript] NVARCHAR(MAX) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallSummary' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoCallSummary] NVARCHAR(MAX) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoProspectDisposition' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoProspectDisposition] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoOnboardingPreference' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoOnboardingPreference] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoKycAccepted' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoKycAccepted] BIT NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCalledBy' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoCalledBy] VARCHAR(150) NULL
END
GO
