IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CheckEmploymentDetailsByMobileNumber'))
BEGIN
    DROP PROCEDURE dbo.usp_CheckEmploymentDetailsByMobileNumber
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_CheckEmploymentDetailsByMobileNumber]
    @MobileNumber VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)
    FROM EmploymentDetails
    WHERE MobileNumber = @MobileNumber
END
GO
