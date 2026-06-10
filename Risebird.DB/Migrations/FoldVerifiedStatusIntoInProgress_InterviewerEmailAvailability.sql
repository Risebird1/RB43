-- Migration: Collapse the InterviewerEmailAvailability status model to three operational states.
--
-- The legacy 'Verified' status only meant "the AI résumé-fitment check returned a positive verdict",
-- which is a pipeline outcome, not an operational state. The status dimension is now strictly
-- Open / InProgress / Closed (which add up to the total). 'Verified' records are folded into
-- 'InProgress'. The AI verdict/score itself is preserved in ResumeAIAnalysisResult and is not lost.

UPDATE dbo.InterviewerEmailAvailability
SET    Status    = 'InProgress',
       UpdatedAt = GETUTCDATE()
WHERE  Status = 'Verified';
GO

-- Defensive: any NULL/blank status becomes the default 'Open'.
UPDATE dbo.InterviewerEmailAvailability
SET    Status = 'Open'
WHERE  Status IS NULL OR LTRIM(RTRIM(Status)) = '';
GO
