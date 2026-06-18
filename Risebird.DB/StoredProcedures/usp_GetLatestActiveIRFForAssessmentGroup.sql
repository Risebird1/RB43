-- Returns the most recent ACTIVE TransInterviewer row id for the given assessment group.
-- NOTE: TransInterviewer has no AssessmentGroupId/IsDeleted columns; the AG association lives
-- on IRF, so this joins TransInterviewer -> IRF and filters on the IRF's assessment group
-- and pause flag. (Replaces the previous inline query that referenced non-existent columns.)
CREATE OR ALTER PROCEDURE [dbo].[usp_GetLatestActiveIRFForAssessmentGroup]
    @AssessmentGroupId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 ti.id
    FROM TransInterviewer ti
    INNER JOIN IRF i ON i.id = ti.IRFId
    WHERE i.AssessmentGroupId = CONVERT(VARCHAR(100), @AssessmentGroupId)
      AND ISNULL(ti.IsActive, 0) = 1
      AND ISNULL(i.IsPaused, 0) = 0
    ORDER BY ti.id DESC;
END
GO
