-- OPTIONAL CLEANUP - run by hand, only after Advanced Resume Screening (IRFScreeningRubric /
-- CandidateAdvancedScreening) is deployed and in use.
--
-- Removes the first, MCQ-ported version of advanced resume screening (commit a18b64720), which
-- the IRF-level design in the RB38 developer handoff replaced. Nothing in the code reads these
-- objects any more. They are left in place by the deployment so this is a deliberate step:
-- the table and the column hold the rubrics and results that version produced, and dropping
-- them is not reversible. Take a backup first if those results are wanted for comparison.

IF OBJECT_ID('dbo.usp_GetIRFResumeScreeningCriteria') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetIRFResumeScreeningCriteria
GO
IF OBJECT_ID('dbo.usp_SaveIRFResumeScreeningCriteria') IS NOT NULL
    DROP PROCEDURE dbo.usp_SaveIRFResumeScreeningCriteria
GO
IF OBJECT_ID('dbo.usp_SaveCandidateAdvancedResumeScreeningResults') IS NOT NULL
    DROP PROCEDURE dbo.usp_SaveCandidateAdvancedResumeScreeningResults
GO
IF OBJECT_ID('dbo.usp_GetCandidatesForAdvancedResumeScreening') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetCandidatesForAdvancedResumeScreening
GO

IF OBJECT_ID('dbo.IRFResumeScreeningCriteria') IS NOT NULL
    DROP TABLE dbo.IRFResumeScreeningCriteria
GO

IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'AIAdvancedScreeningResultJson' AND Object_ID = Object_ID(N'Candidate'))
    ALTER TABLE Candidate DROP COLUMN AIAdvancedScreeningResultJson
GO
