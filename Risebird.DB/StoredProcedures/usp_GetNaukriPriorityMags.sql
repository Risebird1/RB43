-- Returns the MAGs currently flagged as priority for today (IST). Yesterday's flags are
-- automatically excluded by the PriorityDate filter ("resets daily to none").
CREATE OR ALTER PROCEDURE [dbo].[usp_GetNaukriPriorityMags]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);

    SELECT  pm.Id,
            pm.AssessmentGroupId,
            pm.MagName,
            pm.SetBy,
            pm.SetAt
    FROM    NaukriPriorityMag pm
    WHERE   pm.PriorityDate = @Today
      AND   pm.IsActive = 1
    ORDER BY pm.SetAt ASC;
END
GO
