IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ShowBlackListCompany'))
BEGIN
    DROP PROCEDURE dbo.usp_ShowBlackListCompany
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_ShowBlackListCompany
as
begin
select * from BlackListedCompany where IsBlackList=1 
 end
 Go