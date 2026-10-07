IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_BounceDroppedScopeAllows]')
                      AND type IN (N'IF', N'TF', N'FN'))
BEGIN
    DROP FUNCTION dbo.fn_BounceDroppedScopeAllows
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Single source of truth for "which Bounce & Dropped Mail rows may this user see / act on".
-- Used by fn_GetBounceDroppedRows (list + filter options) and usp_CanAccessBounceDroppedRow
-- (server-side check behind Fix Email / Resend / Resolve / Unreachable / mail content), so the
-- buttons can never reach a row the list would not show.
--
-- Returns one row (Allowed = 1) when allowed, no row otherwise; use with CROSS APPLY or EXISTS.
--
-- Client role 1: Candidate.ClientHRAttemptContact = user. Role 4: Candidate.ProfileSource = user.
-- Role 9: every channel of their ClientId. Other client roles: channels mapped in TransClientUser.
-- Admin / SupportAdmin / HR / SeniorHR: everything. HR scoping to IRF.RisebirdHr was removed from the
-- list SP on purpose in 3bc3dc132 (Nov 2025); to restore it, replace the first line of the WHERE with
--     @Role IN ('Admin', 'SupportAdmin')
--     OR (@Role IN ('HR', 'SeniorHR') AND @IRFRisebirdHrEmail = @EmailId)
CREATE FUNCTION [dbo].[fn_BounceDroppedScopeAllows]
(
    @Role                   varchar(150),
    @ClientRoleId           int,
    @ClientId               int,
    @EmailId                varchar(150),
    @IRFId                  int,
    @IRFClientId            int,
    @IRFRisebirdHrEmail     varchar(150),
    @ClientHRAttemptContact varchar(150),
    @ProfileSource          varchar(200)
)
RETURNS TABLE
AS
RETURN
    SELECT 1 AS Allowed
    WHERE @Role IN ('Admin', 'SupportAdmin', 'HR', 'SeniorHR')
       OR (@Role = 'Client' AND (
                (@ClientRoleId = 1 AND @ClientHRAttemptContact = @EmailId)
             OR (@ClientRoleId = 4 AND @ProfileSource = @EmailId)
             OR (@ClientRoleId = 9 AND @IRFClientId = @ClientId)
             OR (ISNULL(@ClientRoleId, 0) NOT IN (1, 4, 9) AND EXISTS (
                    SELECT 1
                    FROM TransClientUser tcu
                    JOIN UserList ulcu ON ulcu.id = tcu.ClientUserId
                    WHERE tcu.IRFId = @IRFId AND ulcu.EmailId = @EmailId))
          ))
GO
