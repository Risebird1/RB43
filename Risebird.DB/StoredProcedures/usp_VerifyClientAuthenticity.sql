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

if(@userClientId = @irfClientId)
Begin
	set @result = 1
End

select @result

End  
  
  
GO


