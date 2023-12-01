/****** Object:  StoredProcedure [dbo].[usp_VerifyClientAuthenticity]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_VerifyClientAuthenticity'))
BEGIN
    DROP PROCEDURE dbo.usp_VerifyClientAuthenticity
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_VerifyClientAuthenticity] 
@emailId varchar(100),
@irfId int
AS  
Begin  
declare @irfClientId int, @userClientId int, @result bit = 0
select @userClientId = ClientId from ClientUser cu join UserList ul on ul.id = cu.UserId
where ul.EmailId = @emailId 
select @irfClientId = ClientId from IRF where id =@irfId

Declare @emaildomain varchar(100),@clientdomain varchar(100)
SELECT @emaildomain=SUBSTRING(@emailId , LEN(@emailId) -  CHARINDEX('@',REVERSE(@emailId)) + 2  , LEN(@emailId)) 
set @clientdomain=(select top(1) SUBSTRING(ul.EmailId , LEN(ul.EmailId) -  CHARINDEX('@',REVERSE(ul.EmailId)) + 2  , LEN(ul.EmailId)) from ClientUser cu join UserList ul on ul.id = cu.UserId
where cu.ClientId=@irfClientId)

if(@clientdomain = @emaildomain)
Begin
	set @result = 1
End
select @result

End  
  
GO


