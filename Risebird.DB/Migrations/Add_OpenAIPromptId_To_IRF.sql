-- =============================================
-- Migration: Add OpenAIPromptId column to IRF table
-- Date: 2026-08-19
-- Description: Stores the Responses API prompt ID (pmpt_…) used for preset
--              AI interview report/evaluation generation for a skill channel.
--              Replaces reliance on OpenAIAssistantId for the Responses path.
--              When null, callers fall back to OpenAI:AIInterviewPromptMap.
-- =============================================

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'OpenAIPromptId'
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    PRINT 'Adding OpenAIPromptId column to IRF table...';
    ALTER TABLE IRF
    ADD OpenAIPromptId VARCHAR(100) NULL;
END
ELSE
BEGIN
    PRINT 'OpenAIPromptId column already exists on IRF table. Skipping.';
END
GO
