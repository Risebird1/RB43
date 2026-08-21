-- =============================================
-- Migration: Backfill IRF.UseAIQuestionSets for channels that already deal papers
-- Date: 2026-08-21
-- Description: Adoption of AI interview question sets used to be inferred from the
--              presence of rows in AIQuestionSets. It is now an explicit per-channel
--              flag, IRF.UseAIQuestionSets.
--
--              THIS MUST RUN WITH THE COLUMN. Without it, every channel that already
--              deals papers reads as "not using sets" and silently reverts to the
--              pre-feature picker, which serves every candidate in a batch the same
--              questions -- exactly the leak the sets exist to prevent.
--
--              Only NULL is touched, so a deliberate 0 (a channel switched off on
--              purpose) survives a re-run. Safe to run repeatedly.
-- =============================================

IF COL_LENGTH('dbo.IRF', 'UseAIQuestionSets') IS NULL
BEGIN
    PRINT 'IRF.UseAIQuestionSets does not exist yet. Apply Tables\IRF.sql first. Skipping.';
END
ELSE IF OBJECT_ID('dbo.AIQuestionSets', 'U') IS NULL
BEGIN
    PRINT 'AIQuestionSets does not exist, so no channel can be dealing papers yet. Nothing to backfill.';
END
ELSE
BEGIN
    DECLARE @Backfilled int;

    UPDATE irf
    SET UseAIQuestionSets = 1
    FROM IRF irf
    WHERE irf.UseAIQuestionSets IS NULL
      AND EXISTS (SELECT 1 FROM AIQuestionSets s WHERE s.IRFId = irf.id);

    SET @Backfilled = @@ROWCOUNT;

    PRINT 'Channels switched on because they already have question sets: ' + CAST(@Backfilled AS varchar(10));
END
GO
