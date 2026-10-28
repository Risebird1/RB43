IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_InterviewerCompletedInterviewsOnIrf]')
                      AND type IN (N'IF', N'TF', N'FN'))
BEGIN
    DROP FUNCTION dbo.fn_InterviewerCompletedInterviewsOnIrf
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Single source of truth for "which interviews has this interviewer completed on this channel".
--
-- The trial subsystem defines a trial interview as the interviewer's first completed interview on a
-- channel. Two readers need that predicate and must never disagree:
--   * usp_GetTrialJudgmentContext      - after the interview, picks the first completed one (rn = 1)
--   * usp_GetInterviewAlignmentContext - before the interview, asks whether any completed one
--                                        precedes this one, to decide whether to show the trial banner
-- Both read this function, so the banner cannot say "trial" while the flag job says otherwise.
--
-- @IrfName is InterviewMeetings.IRFId (the channel code, not IRF.id). @Interviewer is matched the way
-- the trial job always has: trimmed, exact.
CREATE FUNCTION [dbo].[fn_InterviewerCompletedInterviewsOnIrf]
(
    @IrfName     varchar(50),
    @Interviewer varchar(200)
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        im.id,
        im.interviewDate
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.IRFId = @IrfName
      AND LTRIM(RTRIM(im.interviewer)) = @Interviewer
      AND ISNULL(im.confirmationStatus, 0) = 1
      AND (
            ic.MeetingStatus IN (4, 6, 7, 8)
         OR ic.InterviewStatus IN (2, 3, 4, 5)
      )
)
GO
