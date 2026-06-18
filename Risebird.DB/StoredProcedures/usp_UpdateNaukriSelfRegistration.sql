CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateNaukriSelfRegistration]
    @Id        INT,
    @Completed BIT,
    @UserId    INT = NULL,
    @ProfileId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET SelfRegistrationCompleted = @Completed,
        OnboardedUserId           = ISNULL(@UserId, OnboardedUserId),
        OnboardedProfileId        = ISNULL(@ProfileId, OnboardedProfileId),
        OnboardingCompleted       = CASE WHEN @Completed = 1 THEN 1 ELSE OnboardingCompleted END,
        Status                    = CASE WHEN @Completed = 1 THEN 'Onboarded' ELSE Status END,
        UpdatedAt                 = GETUTCDATE()
    WHERE Id = @Id;
END
GO
