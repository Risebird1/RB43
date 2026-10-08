IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_GetBounceDroppedRows]')
                      AND type IN (N'IF', N'TF', N'FN'))
BEGIN
    DROP FUNCTION dbo.fn_GetBounceDroppedRows
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Core row set of the Bounce & Dropped Mail page (one row per bounced/dropped event, no mail body).
-- Shared by usp_GetEmailRecipientsActivityBounceDropped (list) and usp_GetBounceDroppedFilterOptions,
-- so both see exactly the same rows. Callers filter rn = 1 to get the latest event per (email, message).
--
-- @start is inclusive, @endExclusive exclusive (sargable; see IX_SendgridEventData_Event_CreatedAt).
-- Callers should use OPTION (RECOMPILE) so the @recipientType / @Role branches are folded away.
--
-- The recipient survives an email correction: a bounce on OldEmail still resolves to the candidate
-- through CandidateEmailChangeLog after UserList.EmailId was changed by Fix Email.
CREATE FUNCTION [dbo].[fn_GetBounceDroppedRows]
(
    @recipientType  varchar(30),
    @start          datetime,
    @endExclusive   datetime,
    @EmailId        varchar(150),
    @Role           varchar(150),
    @ClientRoleId   int,
    @ClientId       int
)
RETURNS TABLE
AS
RETURN
WITH ev AS (
    SELECT sge.id, sge.email, sge.Split_Message_Id, sge.[event], sge.CreatedAt, sge.[timestamp], sge.[url],
           sge.reason, sge.[status], rcp.UserId AS RecipientUserId,
           ROW_NUMBER() OVER (PARTITION BY sge.email, sge.Split_Message_Id ORDER BY sge.[timestamp] DESC) AS rn,
           COUNT(*) OVER (PARTITION BY sge.email) AS BounceCount
    FROM SendgridEventData sge
    CROSS APPLY (
        SELECT TOP 1 x.UserId
        FROM (
            SELECT ul.id AS UserId, 1 AS Pri, CAST(NULL AS datetime) AS ChangedAt
            FROM UserList ul
            WHERE ul.EmailId = sge.email
              AND ul.idProfile = CASE @recipientType WHEN 'Candidate' THEN 23 WHEN 'Interviewer' THEN 3 END
            UNION ALL
            SELECT cl.UserId, 2, cl.ChangedAt
            FROM CandidateEmailChangeLog cl
            WHERE @recipientType = 'Candidate' AND cl.OldEmail = sge.email
        ) x
        ORDER BY x.Pri, x.ChangedAt DESC
    ) rcp
    WHERE sge.[event] IN ('bounce', 'dropped')
      AND sge.CreatedAt >= @start AND sge.CreatedAt < @endExclusive
      AND sge.email <> 'hire@risebird.io'
)
SELECT ev.id AS SendgridEventId, smd.id AS SentMailDetailsId,
       smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt AS SentOn, smd.SentBy, smd.sg_message_id,
       ev.email, ev.[event] AS latestEvent, ev.CreatedAt AS ActivityDate, ev.[timestamp], ev.[url],
       ev.reason, ev.[status], ev.reason AS BounceReason, ev.BounceCount, ev.rn,
       -- Temporary bounces (4.x.x SMTP status, mailbox full, quota, try again) can be retried on the same address.
       CASE
         WHEN ev.[event] = 'bounce' AND LEFT(LTRIM(ISNULL(ev.[status], '')), 1) = '4' THEN 'Temporary'
         WHEN ev.reason LIKE '%mailbox full%' OR ev.reason LIKE '%quota%' OR ev.reason LIKE '%try again%' THEN 'Temporary'
         -- Blocked: the address is valid but the receiving server refused the message because of the sender (5.7.x policy,
         -- DMARC / SPF / DKIM, From-header alignment, spam or reputation filters). Not for dropped events (e.g. a recipient
         -- who reported spam): those must not be retried.
         WHEN ev.[event] = 'bounce' AND (LEFT(LTRIM(ISNULL(ev.[status], '')), 3) = '5.7' OR ev.reason LIKE '%5.7.%'
               OR ev.reason LIKE '%DMARC%' OR ev.reason LIKE '%SPF%' OR ev.reason LIKE '%DKIM%' OR ev.reason LIKE '%RFC5322%'
               OR ev.reason LIKE '%From: header%' OR ev.reason LIKE '%blocked%' OR ev.reason LIKE '%spam%'
               OR ev.reason LIKE '%reputation%' OR ev.reason LIKE '%policy%') THEN 'Blocked'
         ELSE 'Permanent'
       END AS BounceKind,
       irf.IRFId AS SkillChannelName, im.interviewDate, uli.EmailId AS ChannelOwner,
       ca.id AS CandidateId, ca.ResumeLink AS ResumeLink, ca.Phone AS MobileNumber, irf.CalendlyLink,
       ulc.EmailId AS candidateEmail,
       CASE WHEN ulc.LastName IS NULL THEN ulc.FirstName
            ELSE RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))
       END AS candidateName,
       ca.ClientHRAttemptContact AS CandidateClientHR, irf.EndDate,
       SUBSTRING(irf.CalendlyLink, LEN(irf.CalendlyLink) - CHARINDEX('=', REVERSE(irf.CalendlyLink)) + 2, LEN(irf.CalendlyLink)) AS SchedulerUniqueid,
       (SELECT TOP 1 i.id FROM Interviewer i WHERE i.UserId = ev.RecipientUserId) AS InterviewerId,
       res.[Status] AS ResolutionStatus,
       -- Billing-owner domain: candidates may not use it (CandidateEmailDomainRule); used by the Fix Email modal.
       CASE WHEN CHARINDEX('@', ubo.EmailId) > 0 THEN SUBSTRING(ubo.EmailId, CHARINDEX('@', ubo.EmailId) + 1, 150) END AS ClientDomain
FROM ev
JOIN SentMailDetails smd ON smd.sg_message_id = ev.Split_Message_Id
LEFT JOIN IRF irf ON irf.id = smd.IRFId
LEFT JOIN InterviewMeetings im ON im.id = smd.InterviewId
-- Interviewer tab: show the interview's candidate (bug #6: no longer an inner join, so interviewer bounces without a
-- candidate are not dropped).
-- Candidate tab: of the bounced address's own login and the logins it was corrected / re-linked to
-- (CandidateEmailChangeLog), take the one that has a candidate in THIS mail's channel, own login first. After a
-- Fix Email re-link the channel's candidate belongs to another login, and the row must still find it.
OUTER APPLY (
    SELECT TOP 1 z.id, z.EmailId, z.FirstName, z.LastName
    FROM (
        SELECT u.id, u.EmailId, u.FirstName, u.LastName, x.Pri, x.ChangedAt
        FROM (
            SELECT ev.RecipientUserId AS UserId, 1 AS Pri, CAST(NULL AS datetime) AS ChangedAt
            UNION ALL
            SELECT cl.UserId, 2, cl.ChangedAt FROM CandidateEmailChangeLog cl WHERE cl.OldEmail = ev.email
        ) x
        JOIN UserList u ON u.id = x.UserId
        WHERE @recipientType = 'Candidate'
          AND EXISTS (SELECT 1 FROM CandidatesMaster cmx JOIN Candidate cx ON cx.CandidateMasterId = cmx.id
                      WHERE cmx.UserId = u.id AND cx.IRFId = irf.id)
        UNION ALL
        -- Interviewer tab: the interview's candidate.
        SELECT u.id, u.EmailId, u.FirstName, u.LastName, 3, NULL
        FROM UserList u
        WHERE @recipientType <> 'Candidate' AND u.EmailId = im.candidateEmail
    ) z
    ORDER BY z.Pri, z.ChangedAt DESC
) ulc
LEFT JOIN UserList uli ON uli.id = irf.RisebirdHr
LEFT JOIN UserList ubo ON ubo.id = irf.BillingOwner
OUTER APPLY (
    SELECT TOP 1 c.id, c.ResumeLink, c.Phone, c.ClientHRAttemptContact, c.ProfileSource
    FROM CandidatesMaster cm
    JOIN Candidate c ON c.CandidateMasterId = cm.id AND c.IRFId = irf.id
    WHERE cm.UserId = ulc.id
    ORDER BY c.id DESC
) ca
OUTER APPLY (
    SELECT TOP 1 r.[Status]
    FROM MailBounceResolution r
    WHERE r.SendgridEventId = ev.id
    ORDER BY r.ActionAt DESC, r.Id DESC
) res
CROSS APPLY dbo.fn_BounceDroppedScopeAllows(@Role, @ClientRoleId, @ClientId, @EmailId,
            irf.id, irf.ClientId, uli.EmailId, ca.ClientHRAttemptContact, ca.ProfileSource) sc
WHERE smd.CreatedAt >= @start AND smd.CreatedAt < @endExclusive
  -- Candidate tab keeps its old meaning: only mails that belong to a candidate of the channel.
  AND (@recipientType <> 'Candidate' OR ca.id IS NOT NULL)
GO
