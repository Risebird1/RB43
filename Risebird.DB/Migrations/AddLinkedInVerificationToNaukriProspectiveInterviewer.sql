-- ============================================================================================
-- Migration: LinkedIn background-verification verdict for NaukriOnboarding prospects
--
-- Adds LinkedInUrl / LinkedInMatch / LinkedInConfidence to NaukriProspectiveInterviewer.
-- These are populated at webhook ingestion by the Relay "Naukri Prospective Interviewer
-- Onboarding — LinkedIn Verified" workflow, which searches Google for the prospect's LinkedIn
-- profile and has an AI step decide (YES/NO + confidence) whether it genuinely matches the
-- extracted candidate details. Only verified prospects reach the webhook, so these columns
-- serve as the audit record of WHICH profile matched and HOW confident the check was.
--
-- Idempotent: safe to re-run. Columns are added only if missing.
-- ============================================================================================
SET NOCOUNT ON;

-------------------------------------------------------------------------------------------------
-- 1) Schema: add columns
-------------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LinkedInUrl' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
    ALTER TABLE NaukriProspectiveInterviewer ADD [LinkedInUrl] NVARCHAR(2000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LinkedInMatch' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
    ALTER TABLE NaukriProspectiveInterviewer ADD [LinkedInMatch] VARCHAR(10) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LinkedInConfidence' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
    ALTER TABLE NaukriProspectiveInterviewer ADD [LinkedInConfidence] INT NULL;
GO

-- LinkedInReason: the AI verdict's one-line justification for the match/no-match decision.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LinkedInReason' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
    ALTER TABLE NaukriProspectiveInterviewer ADD [LinkedInReason] NVARCHAR(2000) NULL;
GO

-------------------------------------------------------------------------------------------------
-- 2) Self-verification diagnostics
-------------------------------------------------------------------------------------------------
SELECT
    COUNT(*)                                                              AS TotalProspects,
    SUM(CASE WHEN LinkedInMatch IS NOT NULL THEN 1 ELSE 0 END)           AS WithLinkedInVerdict,
    SUM(CASE WHEN LinkedInMatch = 'YES'     THEN 1 ELSE 0 END)           AS LinkedInMatched
FROM dbo.NaukriProspectiveInterviewer;
GO
