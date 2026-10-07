IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetBounceMailContent'))
BEGIN
    DROP PROCEDURE dbo.usp_GetBounceMailContent
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Stored mail (subject + HTML body) behind a Bounce & Dropped Mail row. Used by the "Content" link
-- (loaded on click instead of embedding every body in the page) and by Resend Mail, which also needs the
-- linked interview's state and details.
-- Access is checked by the caller (usp_GetBounceDroppedRowForAction) before this runs.
CREATE PROCEDURE [dbo].[usp_GetBounceMailContent]
@SendgridEventId int
AS
BEGIN
SET NOCOUNT ON;

-- InterviewState (QA round 1, Q3): Resend must not send a scheduling mail for an interview that is over.
--   None      = the mail is not tied to an interview
--   Missing   = the interview row no longer exists
--   Cancelled = confirmationStatus 2 (or the row is deleted)
--   Past      = start time has passed (interviewDate is local time; + TimeZoneoffset minutes = UTC, IST if unknown)
--   Upcoming  = otherwise; Resend rebuilds the invite from InterviewMeetings (Q4)
SELECT TOP 1 smd.id AS SentMailDetailsId, smd.MailSubject, smd.MailBody, smd.IRFId, smd.InterviewId,
       smd.Cc AS MailCc,
       im.interviewDate AS InterviewDate,
       CASE
         WHEN smd.InterviewId IS NULL THEN 'None'
         WHEN im.id IS NULL THEN 'Missing'
         WHEN CAST(im.confirmationStatus AS varchar(20)) IN ('2', 'Cancelled') OR ISNULL(im.IsDeleted, 0) = 1 THEN 'Cancelled'
         WHEN im.interviewDate IS NULL THEN 'Missing'
         WHEN DATEADD(MINUTE, ISNULL(im.TimeZoneoffset, -330), im.interviewDate) < GETUTCDATE() THEN 'Past'
         ELSE 'Upcoming'
       END AS InterviewState,
       DATEADD(MINUTE, ISNULL(im.TimeZoneoffset, -330), im.interviewDate) AS InterviewStartUtc,
       ISNULL(NULLIF(im.interviewDuration, 0), 60) AS InterviewDurationMinutes,
       im.zoomLink AS ZoomLink,
       im.candidateEmail AS InterviewCandidateEmail
FROM SendgridEventData sge
JOIN SentMailDetails smd ON smd.sg_message_id = sge.Split_Message_Id
LEFT JOIN InterviewMeetings im ON im.id = smd.InterviewId
WHERE sge.id = @SendgridEventId
ORDER BY smd.id DESC;
END
GO
