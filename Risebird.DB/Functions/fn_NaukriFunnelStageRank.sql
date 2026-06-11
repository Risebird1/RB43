-- Canonical ordinal rank of a NaukriOnboarding funnel stage. Mirrors the monotonic pipeline
-- order enforced by AdvanceFunnelStageAsync. Used to filter prospects by milestone:
--   reached X  => rank(current) >= rank(X)
--   not X      => rank(current) <  rank(X)
-- New / NULL / '' / '_none_' all map to 0.
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
        WHEN 'IntentCollection'    THEN 4
        WHEN 'IntentConfirmed'     THEN 5
        WHEN 'EmploymentVerified'  THEN 6
        WHEN 'ResumeFitment'       THEN 7
        WHEN 'AcknowledgementSent' THEN 8
        WHEN 'Onboarded'           THEN 9
        ELSE 0   -- New, '', '_none_', NULL, or any unknown value
    END;
END
GO
