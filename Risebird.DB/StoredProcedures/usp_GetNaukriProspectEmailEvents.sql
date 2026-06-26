IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetNaukriProspectEmailEvents')
    DROP PROCEDURE usp_GetNaukriProspectEmailEvents
GO

-- Returns the SendGrid delivery + engagement events for the two NaukriOnboarding emails sent to a
-- prospect: the INVITATION email and the ACKNOWLEDGEMENT email. This lets the onboarding UI show,
-- per email, whether it was delivered / bounced / deferred / dropped and whether it was opened /
-- clicked, plus the full event timeline — so an operator can tell if a mail actually reached the
-- prospect (and decide whether to try a different address).
--
-- Correlation:
--   * Invitation     subject LIKE 'Risebird Interviewer Opportunity%'   (NaukriOnboardingController.SendInvitationEmail)
--   * Acknowledgement subject LIKE 'Risebird Onboarding Acknowledgement%' (SendAcknowledgementEmailToInterviewer)
-- We take the most recent SentMailDetails row for the recipient per subject, then read its events
-- from SendgridEventData (SendgridEventData.Split_Message_Id = SentMailDetails.sg_message_id).
--
-- Result set 1 (one row per email that was actually sent): EmailType, MailSubject, SentAt,
--   DeliveryEvent, DeliveryReason, DeliveryStatus, DeliveryResponse, HasBounced, Opened, Clicked.
-- Result set 2 (full timeline, both emails): EmailType, Event, Reason, Status, Response, Url, EventAt.
CREATE PROCEDURE [dbo].[usp_GetNaukriProspectEmailEvents]
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Email IS NULL OR LTRIM(RTRIM(@Email)) = ''
        RETURN;

    DECLARE @e NVARCHAR(256) = LOWER(LTRIM(RTRIM(@Email)));

    DECLARE @msgs TABLE (EmailType VARCHAR(20), MsgId VARCHAR(200), MailSubject VARCHAR(500), SentAt DATETIME);

    -- Most recent INVITATION email to this address.
    INSERT INTO @msgs (EmailType, MsgId, MailSubject, SentAt)
    SELECT TOP 1 'Invitation', smd.sg_message_id, smd.MailSubject, smd.CreatedAt
    FROM SentMailDetails smd
    WHERE CHARINDEX(@e, LOWER(smd.[To])) > 0
      AND smd.MailSubject LIKE 'Risebird Interviewer Opportunity%'
    ORDER BY smd.CreatedAt DESC;

    -- Most recent ACKNOWLEDGEMENT email to this address.
    INSERT INTO @msgs (EmailType, MsgId, MailSubject, SentAt)
    SELECT TOP 1 'Acknowledgement', smd.sg_message_id, smd.MailSubject, smd.CreatedAt
    FROM SentMailDetails smd
    WHERE CHARINDEX(@e, LOWER(smd.[To])) > 0
      AND smd.MailSubject LIKE 'Risebird Onboarding Acknowledgement%'
    ORDER BY smd.CreatedAt DESC;

    -- ── Result set 1: per-email delivery summary ────────────────────────────
    SELECT
        m.EmailType,
        m.MailSubject,
        m.SentAt,
        deliv.[event]    AS DeliveryEvent,
        deliv.reason     AS DeliveryReason,
        deliv.[status]   AS DeliveryStatus,
        deliv.[response] AS DeliveryResponse,
        CASE WHEN bnc.cnt > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasBounced,
        CASE WHEN opn.cnt > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS Opened,
        CASE WHEN clk.cnt > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS Clicked
    FROM @msgs m
    OUTER APPLY (
        SELECT TOP 1 sed.[event], sed.reason, sed.[status], sed.[response]
        FROM SendgridEventData sed
        WHERE sed.Split_Message_Id = m.MsgId
          AND sed.[event] IN ('processed', 'delivered', 'deferred', 'bounce', 'dropped')
        ORDER BY COALESCE(sed.CreatedAt, DATEADD(SECOND, TRY_CONVERT(BIGINT, sed.[timestamp]), '1970-01-01')) DESC, sed.id DESC
    ) deliv
    OUTER APPLY (
        SELECT COUNT(*) AS cnt FROM SendgridEventData sed
        WHERE sed.Split_Message_Id = m.MsgId
          AND (sed.[event] LIKE '%bounce%' OR sed.[event] LIKE '%drop%')
    ) bnc
    OUTER APPLY (
        SELECT COUNT(*) AS cnt FROM SendgridEventData sed
        WHERE sed.Split_Message_Id = m.MsgId AND sed.[event] = 'open'
    ) opn
    OUTER APPLY (
        SELECT COUNT(*) AS cnt FROM SendgridEventData sed
        WHERE sed.Split_Message_Id = m.MsgId AND sed.[event] = 'click'
    ) clk;

    -- ── Result set 2: full event timeline for both emails ───────────────────
    SELECT
        m.EmailType,
        sed.[event]    AS [Event],
        sed.reason     AS Reason,
        sed.[status]   AS [Status],
        sed.[response] AS [Response],
        sed.[url]      AS Url,
        COALESCE(sed.CreatedAt, DATEADD(SECOND, TRY_CONVERT(BIGINT, sed.[timestamp]), '1970-01-01')) AS EventAt
    FROM @msgs m
    JOIN SendgridEventData sed ON sed.Split_Message_Id = m.MsgId
    ORDER BY m.EmailType, EventAt;
END
GO
