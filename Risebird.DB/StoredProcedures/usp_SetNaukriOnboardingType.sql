CREATE OR ALTER PROCEDURE [dbo].[usp_SetNaukriOnboardingType]
    @Id             INT,
    @OnboardingType VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET OnboardingType = @OnboardingType,
        UpdatedAt      = GETUTCDATE()
    WHERE Id = @Id;
END
GO
