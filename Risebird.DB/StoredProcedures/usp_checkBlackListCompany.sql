IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_checkBlackListCompany'))
BEGIN
    DROP PROCEDURE dbo.usp_checkBlackListCompany
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_checkBlackListCompany
@CompanyName varchar(100)
AS
Begin
select * From BlackListedCompany where CompanyName=@CompanyName and IsBlackList=1
END
GO
