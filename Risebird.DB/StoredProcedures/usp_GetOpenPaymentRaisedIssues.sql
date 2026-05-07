IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetOpenPaymentRaisedIssues'))
BEGIN
    DROP PROCEDURE dbo.usp_GetOpenPaymentRaisedIssues
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns all open Payment-type tickets with the full context needed for AI review.
-- Includes: raw transcript URL, Transcript-AI, Report-AI, Contradiction-AI,
--           penalty applied, interview date, interviewer join datetime,
--           and the interviewer's own revert-penalty comment.
CREATE PROCEDURE usp_GetOpenPaymentRaisedIssues
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
    -- Ticket fields
    ri.id                       AS TicketId,
    ri.RequestedBy,
    ri.RequestedOn,
    ri.Comment                  AS InterviewerComment,   -- Interviewer's reason for penalty revert
    ri.interviewId,

    -- Interview core
    im.interviewDate,
    im.Interviewer,

    -- Interviewer join time (earliest join record)
    (
        SELECT TOP 1 jt.JoinedOn
        FROM   InterviewMeetingsJoinTime jt
        WHERE  jt.interviewid = im.Id
          AND  jt.interviewer        = im.Interviewer
        ORDER  BY jt.JoinedOn ASC
    )                           AS InterviewerJoinDateTime,

    -- Raw transcript URL (download separately in code)
    ic.AudioTranscriptFileLink  AS RawTranscriptUrl,

        -- AI analysis blobs from QC pipeline
        qa.TranscritAiDetails       AS TranscriptAI,
        qa.ReportAIDetails          AS ReportAI,
        qa.ContradictionJson        AS ContradictionAI,

        -- Merged AI QC details containing the full payment deduction breakdown
        qa.MergedAIQCDetails        AS MergedAIQCDetails

FROM  RaisedIssues ri
INNER JOIN InterviewMeetings        im  ON im.Id           = ri.interviewId
LEFT  JOIN InterviewComments        ic  ON ic.InterviewId  = im.Id
LEFT  JOIN QcInterviewAIevaluation  qa  ON qa.InterviewId  = im.Id
WHERE ri.Status           = 'Open'
  AND ri.RequestType      = 'Payment'
  AND ri.interviewId      IS NOT NULL
  AND ri.AIReviewResponse IS NULL
  AND ri.Interviewid > 0 -- skip tickets already reviewed by AI (e.g. reopened after AI closure)
END
GO
