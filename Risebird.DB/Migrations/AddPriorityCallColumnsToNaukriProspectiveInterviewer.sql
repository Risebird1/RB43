-- Migration: priority-intent-collection call tracking columns.
-- These are written ONLY by the PriorityIntentCallJob and are kept separate from the
-- regular PlivoCallAttempts* anti-spam counters so the priority push never consumes (or is
-- blocked by) the normal 5-total / 2-per-day call budget. PriorityCallLastAt also drives the
-- short between-call cooldown used by usp_GetPriorityIntentCallProspects.

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PriorityCallAttempts' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PriorityCallAttempts] INT NOT NULL DEFAULT 0
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PriorityCallLastAt' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [PriorityCallLastAt] DATETIME NULL
END
GO
