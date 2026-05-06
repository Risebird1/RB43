CREATE OR ALTER PROCEDURE [dbo].[usp_GetActiveIRFsByAssessmentGroupId]
    @AssessmentGroupId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT i.id, i.IRFId, i.AssessmentGroupId, c.Name AS ClientName
    FROM IRF i
    LEFT JOIN Client c ON c.id = i.ClientId
    WHERE i.AssessmentGroupId = CAST(@AssessmentGroupId AS VARCHAR(100))
      AND (i.IsDeleted IS NULL OR i.IsDeleted = 0)
      AND i.EndDate >= GETUTCDATE()
    ORDER BY i.IRFId
END
GO
