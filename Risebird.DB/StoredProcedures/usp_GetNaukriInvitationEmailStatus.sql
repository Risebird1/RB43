IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetNaukriInvitationEmailStatus')
    DROP PROCEDURE usp_GetNaukriInvitationEmailStatus
GO

-- Returns the SendGrid delivery status of the NaukriOnboarding *invitation* email for a given
-- recipient address, so the EmailParsed funnel step can show whether the invitation was
-- delivered / bounced / dropped (and whether it was opened / clicked).
--
-- Correlation: the invitation is sent via SendHelpsupportMail with the subject
-- 'Risebird Interviewer Opportunity — <MAG>'. We find the most recent such SentMailDetails row
-- for the recipient, then read its events from SendgridEventData
-- (SendgridEventData.Split_Message_Id = SentMailDetails.sg_message_id).
--
-- Returns ZERO rows when no invitation has been sent to @Email yet (the UI then shows the
-- parsed email with nothing against it). Editing the prospect's email and resending naturally
-- re-points this lookup at the new address.
CREATE PROCEDURE [dbo].[usp_GetNaukriInvitationEmailStatus]
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Email IS NULL OR LTRIM(RTRIM(@Email)) = ''
        RETURN;

    DECLARE @MsgId VARCHAR(50), @SentAt DATETIME, @Subject VARCHAR(500);

    -- Most recent invitation email sent to this address.
    SELECT TOP 1
        @MsgId   = smd.sg_message_id,
        @SentAt  = smd.CreatedAt,
        @Subject = smd.MailSubject
    FROM SentMailDetails smd
    WHERE CHARINDEX(LOWER(@Email), LOWER(smd.[To])) > 0
      AND smd.MailSubject LIKE 'Risebird Interviewer Opportunity%'
    ORDER BY smd.CreatedAt DESC;

    IF @SentAt IS NULL
        RETURN;   -- invitation not sent yet

    -- Latest delivery-class event (processed / delivered / deferred / bounce / dropped),
    -- plus open / click signals, for this message + recipient.
    SELECT
        @Email                         AS Email,
        @SentAt                        AS SentAt,
        @Subject                       AS MailSubject,
        deliv.[event]                  AS DeliveryEvent,
        deliv.reason                   AS DeliveryReason,
        deliv.[status]                 AS DeliveryStatus,
        deliv.[response]               AS DeliveryResponse,
        CASE WHEN bnc.cnt   > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasBounced,
        CASE WHEN opn.cnt   > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS Opened,
        CASE WHEN clk.cnt   > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS Clicked
    FROM (SELECT 1 AS x) seed
    OUTER APPLY (
        SELECT TOP 1 sed.[event], sed.reason, sed.[status], sed.[response]
        FROM SendgridEventData sed
        WHERE sed.Split_Message_Id = @MsgId
          AND LOWER(sed.email) = LOWER(@Email)
          AND sed.[event] IN ('processed', 'delivered', 'deferred', 'bounce', 'dropped')
        ORDER BY TRY_CONVERT(BIGINT, sed.[timestamp]) DESC, sed.id DESC
    ) deliv
    OUTER APPLY (
        SELECT COUNT(*) AS cnt FROM SendgridEventData sed
        WHERE sed.Split_Message_Id = @MsgId AND LOWER(sed.email) = LOWER(@Email)
          AND (sed.[event] LIKE '%bounce%' OR sed.[event] LIKE '%drop%')
    ) bnc
    OUTER APPLY (
        SELECT COUNT(*) AS cnt FROM SendgridEventData sed
        WHERE sed.Split_Message_Id = @MsgId AND LOWER(sed.email) = LOWER(@Email)
          AND sed.[event] = 'open'
    ) opn
    OUTER APPLY (
        SELECT COUNT(*) AS cnt FROM SendgridEventData sed
        WHERE sed.Split_Message_Id = @MsgId AND LOWER(sed.email) = LOWER(@Email)
          AND sed.[event] = 'click'
    ) clk;
END
GO
