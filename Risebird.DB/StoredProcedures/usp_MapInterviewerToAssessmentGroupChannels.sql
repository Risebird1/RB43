-- Maps an EXISTING interviewer to an Assessment Group WITHOUT rewriting their profile fields.
--
-- Used by the Naukri onboarding "adopt existing interviewer" path: when a prospect's email already
-- maps to an interviewer account, we must not re-create / overwrite that profile. Instead we only:
--   1. Append the AG id to InterviewerProfile.AssessmentGroupId (comma list) if it is missing.
--      This is what surfaces the interviewer in the MAG members popup
--      (usp_GetAssessmentGroupInterviewerMembers) once their account is active.
--   2. Record AssessmentGroupInterviewers membership if missing.
--   3. Add the AG's active skill channels as AVAILABLE ONLY (IsAvailable = 1, IsActive = 0),
--      pending manual operator activation (ActivatedAt stays NULL).
--
-- Fully idempotent: re-running adds nothing that is already present, and never flips an existing
-- channel's IsActive flag.
CREATE OR ALTER PROCEDURE [dbo].[usp_MapInterviewerToAssessmentGroupChannels]
    @InterviewerProfileId INT,
    @UserId               INT,
    @AssessmentGroupId    INT,
    @AssignedBy           VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @InterviewerProfileId IS NULL OR @InterviewerProfileId <= 0
        OR @UserId IS NULL OR @UserId <= 0
        OR @AssessmentGroupId IS NULL OR @AssessmentGroupId <= 0
        RETURN;

    DECLARE @CurrentAGIds VARCHAR(500);
    SELECT @CurrentAGIds = ISNULL(AssessmentGroupId, '')
    FROM InterviewerProfile WHERE Id = @InterviewerProfileId;

    -- 1. Append the AG id to the profile's comma-separated list if not already present.
    IF NOT EXISTS (
        SELECT 1 FROM string_split(@CurrentAGIds, ',')
        WHERE LTRIM(RTRIM(value)) = CAST(@AssessmentGroupId AS VARCHAR)
    )
    BEGIN
        IF @CurrentAGIds = '' OR @CurrentAGIds IS NULL
            SET @CurrentAGIds = CAST(@AssessmentGroupId AS VARCHAR);
        ELSE
            SET @CurrentAGIds = @CurrentAGIds + ',' + CAST(@AssessmentGroupId AS VARCHAR);

        UPDATE InterviewerProfile
        SET AssessmentGroupId = @CurrentAGIds,
            ModifiedOn = GETDATE(),
            ModifiedBy = @AssignedBy
        WHERE Id = @InterviewerProfileId;
    END

    -- 2. Record AG membership.
    IF NOT EXISTS (
        SELECT 1 FROM AssessmentGroupInterviewers
        WHERE AssessmentGroupId = @AssessmentGroupId AND InterviewerProfileId = @InterviewerProfileId
    )
        INSERT INTO AssessmentGroupInterviewers (AssessmentGroupId, InterviewerProfileId, IsSME, CreatedBy, CreatedOn)
        VALUES (@AssessmentGroupId, @InterviewerProfileId, 0, ISNULL(@AssignedBy, 'System'), GETDATE());

    -- 3. Add the AG's active skill channels as AVAILABLE ONLY (inactive, pending manual activation).
    INSERT INTO TransInterviewer (IRFId, InterviewerId, UserId, IsActive, IsAvailable, IsSMEVerified, ClientAction, PerSuccessRate, [Source], AddedAt, ActivatedAt)
    SELECT
        irf.id,
        @InterviewerProfileId,
        @UserId,
        0,   -- IsActive: pending manual operator activation
        1,   -- IsAvailable
        CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN 0 ELSE 1 END,
        CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN NULL ELSE 'Shortlisted' END,
        ISNULL(irf.FlatRate, 0),
        'Naukri-Adopt',
        GETDATE(),
        NULL -- ActivatedAt: set only when an operator activates the channel
    FROM IRF irf
    WHERE LTRIM(RTRIM(irf.AssessmentGroupId)) = CAST(@AssessmentGroupId AS VARCHAR)
      AND ISNULL(irf.IsPaused, 0) = 0
      AND irf.EndDate >= GETDATE()
      AND NOT EXISTS (
          SELECT 1 FROM TransInterviewer ti
          WHERE ti.IRFId = irf.id AND ti.UserId = @UserId
      );

    SELECT 1 AS Success;
END
GO
