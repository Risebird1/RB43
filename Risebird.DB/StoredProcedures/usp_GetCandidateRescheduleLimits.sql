IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCandidateRescheduleLimits'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateRescheduleLimits
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Candidate reschedule cap and completion deadline for one interview (InterviewMeetings.id).
--
-- A reschedule cancels the current InterviewMeetings row and creates a new one, and
-- usp_UpdateReSchedulingInterview increments CandidateRescheduleCount on the row it cancels. So the
-- quota used is the sum across the candidate's rows on this channel, and the first scheduled date is
-- the earliest of those rows.
--
-- MaxCandidateReschedules / InterviewCompleteByDays NULL means no limit; the caller then renders and
-- behaves exactly as before.
Create Procedure usp_GetCandidateRescheduleLimits
@interviewid int
AS
BEGIN
SET NOCOUNT ON;

SELECT
    IRF.MaxCandidateReschedules,
    IRF.InterviewCompleteByDays,
    im.candidateEmail AS CandidateEmail,
    im.interviewDate AS CurrentInterviewDate,
    (SELECT ISNULL(SUM(ISNULL(x.CandidateRescheduleCount, 0)), 0)
       FROM InterviewMeetings x
      WHERE x.IRFId = im.IRFId AND x.candidateEmail = im.candidateEmail) AS CandidateReschedulesUsed,
    (SELECT MIN(x.interviewDate)
       FROM InterviewMeetings x
      WHERE x.IRFId = im.IRFId AND x.candidateEmail = im.candidateEmail AND ISNULL(x.IsDeleted, 0) = 0) AS FirstScheduledDate
FROM InterviewMeetings im
JOIN IRF ON IRF.IRFId = im.IRFId
WHERE im.id = @interviewid
END
GO
