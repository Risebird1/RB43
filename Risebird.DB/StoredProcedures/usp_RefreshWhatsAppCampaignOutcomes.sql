-- WhatsApp campaigns: settles the candidates still Pending or Sent from what has happened since.
--
-- schedule / reschedule (C1 / C2):
--   a meeting on the row's IRF, not cancelled, created after FirstSentAt -> Converted ("Booked <date>")
--   never sent yet and already holding an upcoming meeting                -> NoResponse ("Already booked <date> - not sent")
-- confirm (C3), the row's InterviewId:
--   IsCandidateResponsed = 1: CandidateResponse 2 (not available)  -> Declined
--                             any other answer (yes / reschedule / clarity) -> Converted
--   cancelled or deleted                                           -> NoResponse ("Interview cancelled")
--   interview time passed unanswered                               -> NoResponse ("Interview passed unanswered")
--
-- InterviewMeetings.interviewDate is IST; dtCreatedOn is compared with FirstSentAt (UTC) as stored.
-- Returns the number of rows moved.
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_RefreshWhatsAppCampaignOutcomes'))
BEGIN
    DROP PROCEDURE dbo.usp_RefreshWhatsAppCampaignOutcomes
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_RefreshWhatsAppCampaignOutcomes]
    @CampaignId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now    DATETIME = GETUTCDATE();
    DECLARE @NowIST DATETIME = DATEADD(MINUTE, 330, GETUTCDATE());
    DECLARE @Moved  INT = 0;
    DECLARE @Purpose VARCHAR(20) = (SELECT Purpose FROM dbo.WhatsAppCampaign WHERE Id = @CampaignId);

    IF @Purpose IN ('schedule', 'reschedule')
    BEGIN
        -- Booked from the invite.
        UPDATE cc SET
            [State]     = 'Converted',
            ConvertedAt = @Now,
            NextSendAt  = NULL,
            OutcomeNote = 'Booked ' + FORMAT(booked.interviewDate, 'dd MMM yyyy, h:mm tt', 'en-US'),
            UpdatedAt   = @Now
        FROM dbo.WhatsAppCampaignCandidate cc
        INNER JOIN dbo.IRF irf ON irf.id = cc.IRFId
        CROSS APPLY (
            SELECT TOP (1) im.interviewDate
            FROM dbo.InterviewMeetings im
            WHERE im.candidateEmail = cc.CandidateEmail
              AND im.IRFId = irf.IRFId
              AND ISNULL(im.IsDeleted, 0) = 0
              AND ISNULL(im.confirmationStatus, '') <> '2'
              AND im.dtCreatedOn >= cc.FirstSentAt
            ORDER BY im.dtCreatedOn DESC
        ) booked
        WHERE cc.CampaignId = @CampaignId
          AND cc.[State] IN ('Pending', 'Sent')
          AND cc.FirstSentAt IS NOT NULL;
        SET @Moved = @Moved + @@ROWCOUNT;

        -- Booked some other way before the first send: never send an invite to someone already booked.
        UPDATE cc SET
            [State]     = 'NoResponse',
            NextSendAt  = NULL,
            OutcomeNote = 'Already booked ' + FORMAT(upcoming.interviewDate, 'dd MMM yyyy, h:mm tt', 'en-US') + ' - not sent',
            UpdatedAt   = @Now
        FROM dbo.WhatsAppCampaignCandidate cc
        INNER JOIN dbo.IRF irf ON irf.id = cc.IRFId
        CROSS APPLY (
            SELECT TOP (1) im.interviewDate
            FROM dbo.InterviewMeetings im
            WHERE im.candidateEmail = cc.CandidateEmail
              AND im.IRFId = irf.IRFId
              AND ISNULL(im.IsDeleted, 0) = 0
              AND ISNULL(im.confirmationStatus, '') <> '2'
              AND im.interviewDate > @NowIST
            ORDER BY im.interviewDate ASC
        ) upcoming
        WHERE cc.CampaignId = @CampaignId
          AND cc.[State] = 'Pending'
          AND cc.Sends = 0;
        SET @Moved = @Moved + @@ROWCOUNT;
    END
    ELSE IF @Purpose = 'confirm'
    BEGIN
        UPDATE cc SET
            [State] = CASE
                          WHEN im.id IS NULL OR ISNULL(im.IsDeleted, 0) = 1 OR ISNULL(im.confirmationStatus, '') = '2' THEN 'NoResponse'
                          WHEN ISNULL(im.IsCandidateResponsed, 0) = 1 AND im.CandidateResponse = 2 THEN 'Declined'
                          WHEN ISNULL(im.IsCandidateResponsed, 0) = 1 THEN 'Converted'
                          ELSE 'NoResponse'
                      END,
            ConvertedAt = CASE WHEN im.id IS NOT NULL AND ISNULL(im.IsCandidateResponsed, 0) = 1 THEN @Now ELSE cc.ConvertedAt END,
            NextSendAt  = NULL,
            OutcomeNote = CASE
                              WHEN im.id IS NULL OR ISNULL(im.IsDeleted, 0) = 1 OR ISNULL(im.confirmationStatus, '') = '2' THEN 'Interview cancelled'
                              WHEN ISNULL(im.IsCandidateResponsed, 0) = 1 THEN
                                  CASE im.CandidateResponse
                                      WHEN 1 THEN 'Confirmed'
                                      WHEN 2 THEN 'Not available'
                                      WHEN 3 THEN 'Asked to reschedule'
                                      WHEN 4 THEN 'Needs clarity'
                                      ELSE 'Responded'
                                  END
                              ELSE 'Interview passed unanswered'
                          END,
            UpdatedAt   = @Now
        FROM dbo.WhatsAppCampaignCandidate cc
        LEFT JOIN dbo.InterviewMeetings im ON im.id = cc.InterviewId
        WHERE cc.CampaignId = @CampaignId
          AND cc.[State] IN ('Pending', 'Sent')
          AND (   im.id IS NULL
               OR ISNULL(im.IsDeleted, 0) = 1
               OR ISNULL(im.confirmationStatus, '') = '2'
               OR ISNULL(im.IsCandidateResponsed, 0) = 1
               OR im.interviewDate <= @NowIST);
        SET @Moved = @Moved + @@ROWCOUNT;
    END

    SELECT @Moved;
END
GO
