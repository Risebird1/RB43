IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetOperationalCostFactors'))
    DROP PROCEDURE dbo.usp_GetOperationalCostFactors
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Every operational cost factor, active first, for the maintenance list on the profitability page.
CREATE PROCEDURE usp_GetOperationalCostFactors
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Category, Amount, Currency, Basis, EffectiveFrom, EffectiveTo, IsActive, Notes,
           CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
    FROM OperationalCostFactor
    ORDER BY IsActive DESC, Category, Name, EffectiveFrom DESC;
END
GO
