IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetBounceDroppedRowForAction'))
BEGIN
    DROP PROCEDURE dbo.usp_GetBounceDroppedRowForAction
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Server-side access check + context for the Bounce & Dropped Mail row actions
-- (Fix Email, Resend Mail, Mark Resolved, Unreachable, mail content).
--
-- Returns one row only when the SendgridEventId (+ CandidateId, when given) pair is a bounce/dropped row
-- the user may see under the same scope rule as the list (dbo.fn_BounceDroppedScopeAllows). No row means
-- "Candidate not found or you do not have access." Ids from the browser are never trusted on their own:
-- the candidate must belong to the mail's skill channel and the bounced address must be (or have been,
-- per CandidateEmailChangeLog) that candidate's address.
--
-- @CandidateId NULL (interviewer rows / mail content): the interview's candidate is used for scoping,
-- exactly as the list does for the Interviewer tab.
CREATE PROCEDURE [dbo].[usp_GetBounceDroppedRowForAction]
@SendgridEventId int,
@CandidateId int = null,
@EmailId varchar(150)
AS
BEGIN
SET NOCOUNT ON;

DECLARE @Role varchar(150), @ClientRoleId int, @ClientId int;
SELECT @Role = pr.Name FROM UserList ul JOIN Profile pr ON pr.id = ul.idprofile WHERE ul.EmailId = @EmailId;
IF (@Role = 'Client')
BEGIN
    SELECT TOP 1 @ClientRoleId = cu.RoleId, @ClientId = cu.ClientId
    FROM ClientUser cu JOIN UserList ul ON ul.id = cu.UserId
    WHERE ul.EmailId = @EmailId;
END

SELECT TOP 1
       sge.id AS SendgridEventId, sge.email AS BouncedEmail,
       smd.id AS SentMailDetailsId, smd.IRFId, smd.InterviewId,
       irf.IRFId AS SkillChannelName,
       ca.id AS CandidateId, cu.id AS CandidateUserId, cu.EmailId AS CandidateCurrentEmail,
       res.[Status] AS ResolutionStatus,
       -- Resent and RetriedSameAddress both count for the 10-minute resend cooldown.
       (SELECT MAX(r.ActionAt) FROM MailBounceResolution r
         WHERE r.SendgridEventId = sge.id AND r.[Status] IN ('Resent', 'RetriedSameAddress')) AS LastResentAt,
       -- Same rule as dbo.fn_GetBounceDroppedRows: temporary bounces can be retried on the same address.
       CASE
         WHEN sge.[event] = 'bounce' AND LEFT(LTRIM(ISNULL(sge.[status], '')), 1) = '4' THEN 'Temporary'
         WHEN sge.reason LIKE '%mailbox full%' OR sge.reason LIKE '%quota%' OR sge.reason LIKE '%try again%' THEN 'Temporary'
         ELSE 'Permanent'
       END AS BounceKind,
       sge.reason AS BounceReason
FROM SendgridEventData sge
JOIN SentMailDetails smd ON smd.sg_message_id = sge.Split_Message_Id
LEFT JOIN IRF irf ON irf.id = smd.IRFId
LEFT JOIN InterviewMeetings im ON im.id = smd.InterviewId
LEFT JOIN UserList uli ON uli.id = irf.RisebirdHr
OUTER APPLY (
    SELECT TOP 1 c.id, c.ClientHRAttemptContact, c.ProfileSource, cm.UserId
    FROM Candidate c
    JOIN CandidatesMaster cm ON cm.id = c.CandidateMasterId
    WHERE c.IRFId = irf.id
      AND (
            (@CandidateId IS NOT NULL AND c.id = @CandidateId
                AND (EXISTS (SELECT 1 FROM UserList u WHERE u.id = cm.UserId AND u.EmailId = sge.email)
                  OR EXISTS (SELECT 1 FROM CandidateEmailChangeLog cl WHERE cl.UserId = cm.UserId AND cl.OldEmail = sge.email)))
         OR (@CandidateId IS NULL
                AND EXISTS (SELECT 1 FROM UserList u WHERE u.id = cm.UserId AND u.EmailId = im.candidateEmail))
          )
    ORDER BY c.id DESC
) ca
LEFT JOIN UserList cu ON cu.id = ca.UserId
OUTER APPLY (
    SELECT TOP 1 r.[Status]
    FROM MailBounceResolution r
    WHERE r.SendgridEventId = sge.id
    ORDER BY r.ActionAt DESC, r.Id DESC
) res
CROSS APPLY dbo.fn_BounceDroppedScopeAllows(@Role, @ClientRoleId, @ClientId, @EmailId,
            irf.id, irf.ClientId, uli.EmailId, ca.ClientHRAttemptContact, ca.ProfileSource) sc
WHERE sge.id = @SendgridEventId
  AND sge.[event] IN ('bounce', 'dropped')
  AND (@CandidateId IS NULL OR ca.id IS NOT NULL)
OPTION (RECOMPILE);
END
GO
