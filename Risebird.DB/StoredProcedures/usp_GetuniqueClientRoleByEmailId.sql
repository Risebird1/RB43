    
 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetuniqueClientRoleByEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetuniqueClientRoleByEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO   
CREATE PROCEDURE [dbo].[usp_GetuniqueClientRoleByEmailId]    
    
@emailId varchar(100),
@uniqueid varchar(50)
    
AS    
Begin    
  
select cu.RoleId from ClientUser cu join UserList ul on ul.id = cu.UserId 
join ClientPublicURLs cp on cp.Emailid=ul.EmailId
where ul.EmailId = @emailId  and  cp.uniqueid=@uniqueid
  
End    
    



