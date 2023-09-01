 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_undoBlackListCompany'))
BEGIN
    DROP PROCEDURE dbo.usp_undoBlackListCompany
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_undoBlackListCompany]
@CompanyName varchar(100),
@ModifyDate datetime,
@modifyBy varchar(50)
AS
BEGIN
update BlackListedCompany set IsBlackList=0,ModifyDate=@ModifyDate,ModifyBy=@modifyBy where CompanyName=@CompanyName
END
      
    

 