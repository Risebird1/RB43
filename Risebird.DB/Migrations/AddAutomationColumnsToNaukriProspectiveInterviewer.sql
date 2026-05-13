-- Migration: Add automation tracking columns to NaukriProspectiveInterviewer
-- These columns support the automated prospect onboarding pipeline (timer function).

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AutomationStatus' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [AutomationStatus] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AutomationLastProcessedAt' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [AutomationLastProcessedAt] DATETIME NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallAttemptsTotal' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoCallAttemptsTotal] INT NOT NULL DEFAULT 0
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallAttemptsToday' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoCallAttemptsToday] INT NOT NULL DEFAULT 0
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PlivoCallLastAttemptDate' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PlivoCallLastAttemptDate] DATE NULL
END
GO
