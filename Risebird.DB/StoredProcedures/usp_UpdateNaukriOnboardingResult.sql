IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpdateNaukriOnboardingResult')
    DROP PROCEDURE usp_UpdateNaukriOnboardingResult
GO

CREATE PROCEDURE [dbo].[usp_UpdateNaukriOnboardingResult]
    @Id                         INT,
    @OnboardedUserId            INT             = NULL,
    @OnboardedProfileId         INT             = NULL,
    @AssessmentGroupId          INT             = NULL,
    @AcknowledgementEmailSent   BIT             = 0
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.NaukriProspectiveInterviewer
    SET    OnboardingCompleted       = 1,
           OnboardedUserId           = @OnboardedUserId,
           OnboardedProfileId        = @OnboardedProfileId,
           AssessmentGroupId         = @AssessmentGroupId,
           AcknowledgementEmailSent  = @AcknowledgementEmailSent,
           Status                    = 'Onboarded',
           UpdatedAt                 = GETUTCDATE()
    WHERE  Id = @Id;

    -- Also map to all active IRFs for this AG
    IF @OnboardedProfileId IS NOT NULL AND @AssessmentGroupId IS NOT NULL
    BEGIN
        DECLARE @UserId INT
        SELECT @UserId = UserId FROM InterviewerProfile WHERE Id = @OnboardedProfileId

        -- Channel activation follows each channel's Interviewer Lock (IRF.DmIvLockNew):
        --   Locked  (DmIvLockNew = 1) → Available-only (IsActive = 0) pending manual RB vetting.
        --   Unlocked(DmIvLockNew = 0) → auto-activated (IsActive = 1, RB-vetted, Client-shortlisted) now.
        -- Mirrors usp_MapInterviewerToAssessmentGroupChannels / usp_AddOrEditInterviewerProfile (MAG-Direct).
        INSERT INTO TransInterviewer (IRFId, InterviewerId, UserId, IsActive, IsAvailable, IsSMEVerified, ClientAction, PerSuccessRate, [Source], AddedAt, ActivatedAt)
        SELECT
            irf.id,
            @OnboardedProfileId,
            @UserId,
            CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN 0 ELSE 1 END,   -- IsActive
            1,                                                            -- IsAvailable
            CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN 0 ELSE 1 END,   -- IsSMEVerified
            CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN NULL ELSE 'Shortlisted' END,  -- ClientAction
            ISNULL(irf.FlatRate, 0),
            'NaukriOnboarding',
            GETDATE(),
            CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN NULL ELSE GETDATE() END        -- ActivatedAt (set when auto-activated)
        FROM IRF irf
        WHERE LTRIM(RTRIM(irf.AssessmentGroupId)) = CAST(@AssessmentGroupId AS VARCHAR)
          AND ISNULL(irf.IsPaused, 0) = 0
          AND irf.EndDate >= GETDATE()
          AND NOT EXISTS (
              SELECT 1 FROM TransInterviewer ti
              WHERE ti.IRFId = irf.id AND ti.UserId = @UserId
          )
    END

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
