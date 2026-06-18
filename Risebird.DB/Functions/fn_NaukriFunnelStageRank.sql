-- Canonical ordinal rank of a NaukriOnboarding funnel stage. Mirrors the monotonic pipeline
-- order enforced by AdvanceFunnelStageAsync. Used to filter prospects by milestone:
--   reached X  => rank(current) >= rank(X)
--   not X      => rank(current) <  rank(X)
--
-- Only the LINEAR happy-path stages carry a positive rank. Terminal "exit" stages
-- (Duplicate, NotInterested, Unsubscribe, Cold) and New/NULL/''/'_none_' all map to 0 —
-- they represent prospects that have not progressed along the happy path and are filtered
-- as exact-match categories ('At stage'), not via the reached/not-reached milestone modes.
CREATE OR ALTER FUNCTION [dbo].[fn_NaukriFunnelStageRank]
(
    @Stage VARCHAR(50)
)
RETURNS INT
AS
BEGIN
    RETURN CASE ISNULL(@Stage, '')
        WHEN 'Parsed'              THEN 1
        WHEN 'MAGMapped'           THEN 2
        WHEN 'InvitationSent'      THEN 3
        WHEN 'AwaitingResponse'    THEN 4
        WHEN 'IntentConfirmed'     THEN 5
        WHEN 'EmploymentVerified'  THEN 6
        WHEN 'ResumeFitment'       THEN 7
        WHEN 'AcknowledgementSent' THEN 8
        WHEN 'Onboarded'           THEN 9
        ELSE 0   -- New / Duplicate / NotInterested / Unsubscribe / Cold / '_none_' / NULL
    END;
END
GO
