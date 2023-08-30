IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertBlackListCompany'))
BEGIN
    DROP PROCEDURE [dbo].[usp_InsertBlackListCompany]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure [dbo].[usp_InsertBlackListCompany]
@Comment varchar(150),
@CreatedDate datetime ,
@CreatedBy varchar(50),
@ModifyDate DateTime,
@ModifyBy varchar(50),
@CompanyName varchar(100)
as
begin
if exists(select distinct * from BlackListedCompany where CompanyName=@CompanyName)
begin
update BlackListedCompany set Comment=@Comment,ModifyDate=@ModifyDate,ModifyBy=@ModifyBy,IsBlackList=1 where CompanyName=@CompanyName
end
else
begin
Insert into BlackListedCompany(IsBlackList,Comment,CreatedDate,CreatedBy,CompanyName) Values(1,@Comment,@CreatedDate,@CreatedBy,@CompanyName)
end
End