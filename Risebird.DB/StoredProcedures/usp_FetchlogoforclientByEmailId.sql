IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchlogoforclientByEmailId'))
BEGIN
    DROP PROCEDURE [dbo].[usp_FetchlogoforclientByEmailId]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_FetchlogoforclientByEmailId]  
@emailId varchar(100)
          
AS          
Begin          
        
select cp.LogoLink from ClientUser cu join UserList ul on ul.id = cu.UserId       
join Client cp on cp.id=cu.ClientId  
where ul.EmailId = @emailId
        
End 


go