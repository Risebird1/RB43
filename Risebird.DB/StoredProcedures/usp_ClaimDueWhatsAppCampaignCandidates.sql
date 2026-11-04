-- WhatsApp campaigns: claims the candidates to send to now and locks them, so Send now, a per-row Re-send
-- and the scheduler can never send to the same candidate twice at the same time.
--
--   @CampaignId          : WhatsAppCampaign.Id
--   @Take                : at most this many rows (the campaign's BatchSize)
--   @LockMinutes         : how long the claim holds (a crash mid-send frees the row after this)
--   @CampaignCandidateId : claim this one row only (Re-send); it must be Pending
--
-- Due = Pending, NextSendAt passed (or not set) and not locked by another sender. Oldest due first.
-- Returns the claimed rows.
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ClaimDueWhatsAppCampaignCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_ClaimDueWhatsAppCampaignCandidates
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ClaimDueWhatsAppCampaignCandidates]
    @CampaignId          INT,
    @Take                INT,
    @LockMinutes         INT = 5,
    @CampaignCandidateId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME = GETUTCDATE();

    ;WITH due AS (
        SELECT TOP (@Take) *
        FROM dbo.WhatsAppCampaignCandidate WITH (UPDLOCK, READPAST, ROWLOCK)
        WHERE CampaignId = @CampaignId
          AND [State] = 'Pending'
          AND (@CampaignCandidateId IS NULL OR Id = @CampaignCandidateId)
          AND (@CampaignCandidateId IS NOT NULL OR NextSendAt IS NULL OR NextSendAt <= @Now)
          AND (SendLockUntil IS NULL OR SendLockUntil < @Now)
        ORDER BY ISNULL(NextSendAt, CreatedAt), Id
    )
    UPDATE due SET SendLockUntil = DATEADD(MINUTE, @LockMinutes, @Now)
    OUTPUT inserted.*;
END
GO
