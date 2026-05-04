CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateNaukriSelectedIRF]
    @Id              INT,
    @SelectedIRFId   INT,
    @SelectedIRFName NVARCHAR(300),
    @ApplicationLink NVARCHAR(2000),
    @AssessmentGroupId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET SelectedIRFId   = @SelectedIRFId,
        SelectedIRFName = @SelectedIRFName,
        ApplicationLink = @ApplicationLink,
        AssessmentGroupId = ISNULL(@AssessmentGroupId, AssessmentGroupId),
        UpdatedAt       = GETUTCDATE()
    WHERE Id = @Id
END
GO
