-- Migration: unify availability intent on InterviewerEmailAvailability.
--
-- Previously the interviewer's intent lived in two disconnected places:
--   * Email (Relay) replies  -> AvailabilityIntent / MailBodyIntent / ConcernsOrNotInterested
--   * Plivo voice agent call  -> PlivoIntent (available / not_available / no_answer / unsubscribe)
-- The dashboard "Intent" column read only the email columns, so call-driven records
-- showed blank. These columns introduce a single canonical intent fed by BOTH sources,
-- mirroring the unified-intent model already used by NaukriOnboarding.
--
-- Canonical Intent values: 'Available' | 'NotAvailable' | 'Unsubscribe'

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Intent' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [Intent] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IntentSource' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [IntentSource] VARCHAR(20) NULL   -- 'Email' | 'Call'
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IntentConfirmedAt' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [IntentConfirmedAt] DATETIME NULL
END
GO

-- ── Backfill existing rows ───────────────────────────────────────────────────
-- A completed Plivo call disposition is an explicit signal, so it takes priority
-- when present; otherwise we derive intent from the email-extracted fields.

-- 1) From the Plivo call disposition
UPDATE InterviewerEmailAvailability
SET Intent            = CASE LOWER(LTRIM(RTRIM(PlivoIntent)))
                            WHEN 'available'    THEN 'Available'
                            WHEN 'interested'   THEN 'Available'
                            WHEN 'not_available' THEN 'NotAvailable'
                            WHEN 'unsubscribe'  THEN 'Unsubscribe'
                        END,
    IntentSource      = 'Call',
    IntentConfirmedAt = ISNULL(PlivoCallAt, UpdatedAt)
WHERE Intent IS NULL
  AND PlivoIntent IS NOT NULL
  AND LOWER(LTRIM(RTRIM(PlivoIntent))) IN ('available', 'interested', 'not_available', 'unsubscribe');
GO

-- 2) From the email-extracted intent (only where the call did not already classify it)
UPDATE InterviewerEmailAvailability
SET Intent            = CASE
                            WHEN LOWER(ISNULL(AvailabilityIntent, '') + ' ' + ISNULL(MailBodyIntent, '') + ' ' + ISNULL(ConcernsOrNotInterested, '')) LIKE '%unsubscribe%'
                                THEN 'Unsubscribe'
                            WHEN dbo.fn_IsFavorableAvailabilityIntent(AvailabilityIntent, MailBodyIntent, ConcernsOrNotInterested) = 1
                                THEN 'Available'
                            WHEN (ISNULL(LTRIM(RTRIM(ConcernsOrNotInterested)), '') <> ''
                                  AND LOWER(LTRIM(RTRIM(ConcernsOrNotInterested))) NOT IN ('none','no','n/a','na','nil'))
                                 OR LOWER(ISNULL(AvailabilityIntent, '') + ' ' + ISNULL(MailBodyIntent, '')) LIKE '%not available%'
                                 OR LOWER(ISNULL(AvailabilityIntent, '') + ' ' + ISNULL(MailBodyIntent, '')) LIKE '%unavailable%'
                                 OR LOWER(ISNULL(AvailabilityIntent, '') + ' ' + ISNULL(MailBodyIntent, '')) LIKE '%not interested%'
                                THEN 'NotAvailable'
                            ELSE NULL
                        END,
    IntentSource      = 'Email',
    IntentConfirmedAt = ReceivedAt
WHERE Intent IS NULL
  AND (AvailabilityIntent IS NOT NULL OR MailBodyIntent IS NOT NULL OR ConcernsOrNotInterested IS NOT NULL);
GO

-- Clear IntentSource where nothing could be classified (keeps the column tidy)
UPDATE InterviewerEmailAvailability
SET IntentSource = NULL, IntentConfirmedAt = NULL
WHERE Intent IS NULL;
GO
