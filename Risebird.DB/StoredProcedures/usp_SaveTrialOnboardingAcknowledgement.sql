IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveTrialOnboardingAcknowledgement'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SaveTrialOnboardingAcknowledgement]
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================================================
-- Records the interviewer's one-time acknowledgement of the trial terms.
--
-- The UPDATE is guarded by ISNULL(TrialOnboardingAcknowledged, 0) = 0, so a repeat call
-- never overwrites the FIRST acknowledgement's timestamp or policy version. That row is a
-- consent record - when it was agreed, and to which version of the policy.
--
-- The returned bit reflects the CURRENT STORED STATE, not the rowcount, so a repeat call
-- still returns 1 and the caller sees success rather than a spurious failure.
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_SaveTrialOnboardingAcknowledgement]
    @InterviewerEmail varchar(200),
    @PolicyVersion    varchar(50) = null,
    @TimeNow          datetime    = null
AS
BEGIN
    SET NOCOUNT ON;

    IF @TimeNow IS NULL SET @TimeNow = GETDATE();

    UPDATE ip
       SET ip.TrialOnboardingAcknowledged   = 1,
           ip.TrialOnboardingAcknowledgedOn = @TimeNow,
           ip.TrialOnboardingPolicyVersion  = @PolicyVersion
      FROM InterviewerProfile ip
      JOIN UserList ul ON ul.id = ip.UserId
     WHERE ul.EmailId = @InterviewerEmail
       AND ISNULL(ip.TrialOnboardingAcknowledged, 0) = 0;

    SELECT CAST(ISNULL(MAX(CAST(ISNULL(ip.TrialOnboardingAcknowledged, 0) AS int)), 0) AS bit) AS Acknowledged
      FROM InterviewerProfile ip
      JOIN UserList ul ON ul.id = ip.UserId
     WHERE ul.EmailId = @InterviewerEmail;
END
GO
