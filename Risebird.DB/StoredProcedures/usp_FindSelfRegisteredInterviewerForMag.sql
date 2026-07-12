-- Resolves the interviewer account (UserList + InterviewerProfile) for an email ONLY when that
-- interviewer is genuinely mapped to a specific assessment group (MAG). This is the accurate signal
-- for "self-registered via the MAG application link".
--
-- Background / why this exists:
--   The older usp_FindActiveUserByEmail matched ANY active UserList row by email — it did not require
--   an interviewer profile and did not check the MAG. That produced FALSE self-registrations:
--     • a Naukri prospect whose email happened to match a pre-existing interviewer in a DIFFERENT MAG
--       was marked "self-registered → Onboarded" and linked to that other-role account, and
--     • a non-interviewer account with a matching email was linked as the onboarded "panel".
--   A REAL MAG self-registration (usp_AddInternalInterviewerToIRFPostPositiveVerdict, @isMagApplication=1)
--   always leaves two durable traces for the applied AG: (a) TransInterviewer channel rows for that
--   AG's IRFs, and (b) the AG id appended to InterviewerProfile.AssessmentGroupId. We look for either.
CREATE OR ALTER PROCEDURE [dbo].[usp_FindSelfRegisteredInterviewerForMag]
    @Email             VARCHAR(150),
    @AssessmentGroupId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @AssessmentGroupId IS NULL OR @AssessmentGroupId <= 0
        RETURN;

    SELECT TOP 1
        ul.id AS UserId,
        ip.Id AS ProfileId
    FROM UserList ul
    INNER JOIN InterviewerProfile ip ON ip.UserId = ul.id   -- must be a genuine interviewer
    WHERE LOWER(LTRIM(RTRIM(ul.EmailId))) = LOWER(LTRIM(RTRIM(@Email)))
      AND ul.IsActive = 1
      AND (
            -- (a) the confirmed AG id is present in the profile's comma-separated AG list
            EXISTS (
                SELECT 1 FROM STRING_SPLIT(ISNULL(ip.AssessmentGroupId, ''), ',')
                WHERE LTRIM(RTRIM(value)) = CAST(@AssessmentGroupId AS VARCHAR)
            )
            -- (b) OR they hold a skill-channel (TransInterviewer) row for an IRF in the confirmed AG
            OR EXISTS (
                SELECT 1
                FROM TransInterviewer ti
                INNER JOIN IRF irf ON irf.id = ti.IRFId
                WHERE ti.UserId = ul.id
                  AND LTRIM(RTRIM(irf.AssessmentGroupId)) = CAST(@AssessmentGroupId AS VARCHAR)
            )
      );
END
GO
