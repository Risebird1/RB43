/*
  Prerequisite for: dbo.usp_CandidateFeedbackDetails (InterviewerVideo parameter).

  Run this on each database BEFORE deploying / creating the updated stored procedure.
  Safe to re-run: adds InterviewerVideo only if the column is missing.

  Order: 1) this script  2) Risebird.DB\StoredProcedures\usp_CandidateFeedbackDetails.sql
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'CandidateFeedbackDetails'
      AND c.name = N'InterviewerVideo'
)
BEGIN
    ALTER TABLE dbo.CandidateFeedbackDetails
    ADD InterviewerVideo VARCHAR(250) NULL;
END
GO
