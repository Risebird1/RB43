-- =============================================
-- Migration: Add OpenAIEvaluatorPromptId column to IRF table
-- Date: 2026-08-27
-- Description: AI interview prompts are split in two for a skill channel:
--
--                OpenAIPromptId          - Primary Prompt ID. For adaptive AI interviews
--                                          this is the question generator that conducts the
--                                          interview. Mandatory when IsAIInterview = 1.
--                OpenAIEvaluatorPromptId - Interview Evaluator Prompt ID. Generates the
--                                          candidate's report. Optional: when null, the
--                                          Primary Prompt ID generates the report too.
--
--              Preset question generation is unchanged - preset papers still come from
--              the skill channel's question bank / question sets, not from a prompt.
--
--              Backfill: before this change the single OpenAIPromptId column held the
--              REPORT/evaluation prompt (it was labelled "Report Prompt Id" and was read
--              only by the preset report path). Copying it into the new evaluator column
--              pins that existing behaviour down explicitly, so that later repointing
--              Primary at a question-generator prompt cannot silently change how an
--              already-live channel's reports are generated.
-- =============================================

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'OpenAIEvaluatorPromptId'
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    PRINT 'Adding OpenAIEvaluatorPromptId column to IRF table...';
    ALTER TABLE IRF
    ADD OpenAIEvaluatorPromptId VARCHAR(100) NULL;
END
ELSE
BEGIN
    PRINT 'OpenAIEvaluatorPromptId column already exists on IRF table. Skipping.';
END
GO

-- Backfill. Re-runnable: only touches rows whose evaluator column is still empty,
-- so a second execution cannot overwrite a value an admin has since set by hand.
UPDATE IRF
SET OpenAIEvaluatorPromptId = LTRIM(RTRIM(OpenAIPromptId))
WHERE ISNULL(IsAIInterview, 0) = 1
  AND OpenAIPromptId IS NOT NULL
  AND LTRIM(RTRIM(OpenAIPromptId)) <> ''
  AND (OpenAIEvaluatorPromptId IS NULL OR LTRIM(RTRIM(OpenAIEvaluatorPromptId)) = '');
GO

PRINT 'Backfill complete. Rows carrying an evaluator prompt id:';
SELECT COUNT(*) AS ChannelsWithEvaluatorPromptId
FROM IRF
WHERE OpenAIEvaluatorPromptId IS NOT NULL AND LTRIM(RTRIM(OpenAIEvaluatorPromptId)) <> '';
GO
