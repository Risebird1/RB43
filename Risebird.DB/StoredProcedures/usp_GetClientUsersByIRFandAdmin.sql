IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientUsersByIRFandAdmin'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientUsersByIRFandAdmin
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetClientUsersByIRFandAdmin  
@irfId int  
AS  
BEGIN  
Declare @Clientid int  
Select @Clientid=ClientId From IRF Where id=@irfId  
Select ul.EmailId From TransClientUser tcu join UserList ul on ul.id=tcu.ClientUserId where IRFId=@irfId and ul.IsActive=1 
Union  
Select ul.EmailId From CLientUser cu join UserList ul on ul.id=cu.UserId where cu.RoleId=9 and ClientId=@Clientid and ul.IsActive=1  
END
GO