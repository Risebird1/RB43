-- Maps any stored FunnelStage value (including pre-revamp legacy values and NULL/blank)
-- to its canonical value in the current, simplified funnel model. This keeps the dashboard
-- breakdown reconciling to the total and keeps the funnel-stage filter working even for rows
-- that have not yet been touched by the RevampNaukriFunnelStagesV2 data migration.
--
-- Canonical values:
--   New, EmailParsed, PhoneParsed, DuplicateChecked, MAGConfirmed, InterestCollection,
--   IntentConfirmed, EmploymentVerified, ResumeFitment, AcknowledgementSent, Onboarded,
--   Duplicate, NotInterested, Cold, NotQualified
--
-- Legacy -> canonical:
--   ''/NULL                              -> New
--   Parsed                               -> EmailParsed   (the migration splits Email/Phone by ParsedPhone)
--   MAGMapped                            -> MAGConfirmed
--   InvitationSent / AwaitingResponse
--     / IntentCollection                 -> InterestCollection
--   Unsubscribe                          -> NotInterested
CREATE OR ALTER FUNCTION [dbo].[fn_NaukriCanonicalFunnelStage]
(
    @Stage VARCHAR(50)
)
RETURNS VARCHAR(50)
AS
BEGIN
    DECLARE @s VARCHAR(50) = LTRIM(RTRIM(ISNULL(@Stage, '')));

    RETURN CASE
        WHEN @s = ''                                                  THEN 'New'
        WHEN @s = 'Parsed'                                            THEN 'EmailParsed'
        WHEN @s = 'MAGMapped'                                         THEN 'MAGConfirmed'
        WHEN @s IN ('InvitationSent', 'AwaitingResponse', 'IntentCollection') THEN 'InterestCollection'
        WHEN @s = 'Unsubscribe'                                       THEN 'NotInterested'
        ELSE @s
    END;
END
GO
