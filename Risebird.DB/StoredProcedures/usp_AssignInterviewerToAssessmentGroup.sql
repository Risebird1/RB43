IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_AssignInterviewerToAssessmentGroup')
    DROP PROCEDURE usp_AssignInterviewerToAssessmentGroup
GO

CREATE PROCEDURE [dbo].[usp_AssignInterviewerToAssessmentGroup]
    @EmailAvailabilityId    INT,
    @InterviewerProfileId   INT,
    @AssessmentGroupId      INT,
    @AssignedBy             VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentAGIds VARCHAR(500)
    DECLARE @UserId INT

    SELECT @CurrentAGIds = ISNULL(AssessmentGroupId, ''), @UserId = UserId
    FROM InterviewerProfile WHERE Id = @InterviewerProfileId

    -- 1. Update InterviewerProfile.AssessmentGroupId (comma-separated)
    IF NOT EXISTS (
        SELECT 1 FROM string_split(@CurrentAGIds, ',')
        WHERE LTRIM(RTRIM(value)) = CAST(@AssessmentGroupId AS VARCHAR)
    )
    BEGIN
        IF @CurrentAGIds = '' OR @CurrentAGIds IS NULL
            SET @CurrentAGIds = CAST(@AssessmentGroupId AS VARCHAR)
        ELSE
            SET @CurrentAGIds = @CurrentAGIds + ',' + CAST(@AssessmentGroupId AS VARCHAR)

        UPDATE InterviewerProfile
        SET AssessmentGroupId = @CurrentAGIds,
            ModifiedOn = GETDATE(),
            ModifiedBy = @AssignedBy
        WHERE Id = @InterviewerProfileId
    END

    -- 2. Insert into AssessmentGroupInterviewers if not already present
    IF NOT EXISTS (
        SELECT 1 FROM AssessmentGroupInterviewers
        WHERE AssessmentGroupId = @AssessmentGroupId AND InterviewerProfileId = @InterviewerProfileId
    )
    BEGIN
        INSERT INTO AssessmentGroupInterviewers (AssessmentGroupId, InterviewerProfileId, IsSME, CreatedBy, CreatedOn)
        VALUES (@AssessmentGroupId, @InterviewerProfileId, 0, ISNULL(@AssignedBy, 'System'), GETDATE())
    END

    -- 3. Map interviewer to all active IRFs belonging to this AssessmentGroupId.
    --    Channel activation follows each channel's Interviewer Lock (IRF.DmIvLockNew):
    --      Locked   → Available-only (IsActive = 0) pending manual RB vetting (ActivatedAt NULL).
    --      Unlocked → auto-activated (IsActive = 1) immediately (ActivatedAt set now).
    INSERT INTO TransInterviewer (IRFId, InterviewerId, UserId, IsActive, IsAvailable, IsSMEVerified, ClientAction, PerSuccessRate, [Source], AddedAt, ActivatedAt)
    SELECT
        irf.id,
        @InterviewerProfileId,
        @UserId,
        CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN 0 ELSE 1 END,   -- IsActive
        1,   -- IsAvailable
        CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN 0 ELSE 1 END,
        CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN NULL ELSE 'Shortlisted' END,
        ISNULL(irf.FlatRate, 0),
        'EmailAvailability',
        GETDATE(),
        CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN NULL ELSE GETDATE() END -- ActivatedAt: set when auto-activated
    FROM IRF irf
    WHERE LTRIM(RTRIM(irf.AssessmentGroupId)) = CAST(@AssessmentGroupId AS VARCHAR)
      AND ISNULL(irf.IsPaused, 0) = 0
      AND irf.EndDate >= GETDATE()
      AND NOT EXISTS (
          SELECT 1 FROM TransInterviewer ti
          WHERE ti.IRFId = irf.id AND ti.UserId = @UserId
      )

    -- 4. Update the email availability record
    UPDATE InterviewerEmailAvailability
    SET AssessmentGroupId          = @AssessmentGroupId,
        AssessmentGroupAssignedAt  = GETUTCDATE(),
        Status                     = 'Closed',
        ProcessedBy                = @AssignedBy,
        UpdatedAt                  = GETUTCDATE()
    WHERE Id = @EmailAvailabilityId

    SELECT 1 AS Success
END
GO
