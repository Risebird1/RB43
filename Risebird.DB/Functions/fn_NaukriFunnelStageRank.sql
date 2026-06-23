-- Canonical ordinal rank of a NaukriOnboarding funnel stage. Mirrors the monotonic pipeline
-- order enforced by usp_AdvanceNaukriFunnelStage. Used only by the forward-only advance guard.
--
-- Revised funnel model (mirrors the operator screenshot — kept deliberately simple/linear):
--   1  EmailParsed         — invitation email parsed (delivery status shown from SendgridEventData)
--   2  PhoneParsed         — phone parsed & sanitised
--   3  DuplicateChecked    — email confirmed NOT already in the system
--   4  MAGConfirmed        — MAG confirmed/locked by operator or automation
--   5  InterestCollection  — invitation sent; collecting interest (email options / Plivo call)
--   6  IntentConfirmed     — POSITIVE intent confirmed (Interested)
--   7  EmploymentVerified  — employment verification passed
--   8  ResumeFitment       — AI résumé fitment passed
--   9  AcknowledgementSent — acknowledgement email sent (NOT yet onboarded)
--   10 Onboarded           — interviewer accepted acknowledgement & exists in UserList
--
-- Terminal exits (rank 0) — off the happy path, set unconditionally via usp_SetNaukriFunnelOutcome:
--   Duplicate, NotInterested (incl. Drop/Unsubscribe), Cold, NotQualified, New/NULL/''/'_none_'.
CREATE OR ALTER FUNCTION [dbo].[fn_NaukriFunnelStageRank]
(
    @Stage VARCHAR(50)
)
RETURNS INT
AS
BEGIN
    RETURN CASE ISNULL(@Stage, '')
        WHEN 'EmailParsed'         THEN 1
        WHEN 'PhoneParsed'         THEN 2
        WHEN 'DuplicateChecked'    THEN 3
        WHEN 'MAGConfirmed'        THEN 4
        WHEN 'InterestCollection'  THEN 5
        WHEN 'IntentConfirmed'     THEN 6
        WHEN 'EmploymentVerified'  THEN 7
        WHEN 'ResumeFitment'       THEN 8
        WHEN 'AcknowledgementSent' THEN 9
        WHEN 'Onboarded'           THEN 10
        ELSE 0   -- New / Duplicate / NotInterested / Unsubscribe / Cold / NotQualified / '_none_' / NULL
    END;
END
GO
