IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AcknowledgeTrialOnboarding'))
BEGIN
    DROP PROCEDURE dbo.usp_AcknowledgeTrialOnboarding
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- One-time trial onboarding acknowledgement from the interview alignment page. Same shape as
-- usp_updateRedmarkerAcknowledge, keyed on the signed-in interviewer's email rather than a uniqueid
-- taken from the request.
--
-- Only rows not yet acknowledged are touched, so the first acknowledgement timestamp and the policy
-- version it was given against survive any later call.
CREATE PROCEDURE [dbo].[usp_AcknowledgeTrialOnboarding]
    @InterviewerEmail VARCHAR(200),
    @PolicyVersion    VARCHAR(50),
    @CurrentDate      DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ip
       SET TrialOnboardingAcknowledged = 1,
           TrialOnboardingAcknowledgedOn = ISNULL(ip.TrialOnboardingAcknowledgedOn, @CurrentDate),
           TrialOnboardingPolicyVersion = ISNULL(ip.TrialOnboardingPolicyVersion, @PolicyVersion)
      FROM dbo.InterviewerProfile ip
      JOIN dbo.UserList ul ON ul.id = ip.UserId
     WHERE LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(@InterviewerEmail))
       AND ISNULL(ip.TrialOnboardingAcknowledged, 0) = 0;

    SELECT @@ROWCOUNT AS UpdatedRows;
END
GO
