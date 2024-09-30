IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAllCountryCodeDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllCountryCodeDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetAllCountryCodeDetails
AS
BEGIN
Select * from Country_Classification
END
GO