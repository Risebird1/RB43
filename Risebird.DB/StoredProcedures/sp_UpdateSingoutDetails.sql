
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_UpdateSingoutDetails'))
BEGIN
    DROP PROCEDURE dbo.sp_UpdateSingoutDetails
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_UpdateSingoutDetails]  
  @EmailId varchar(200),  
  @SessionId varchar(500)
AS    
  BEGIN     
  UPDATE [dbo].[UserList]   SET      
   LastLogout = GETDATE() 
   WHERE EmailId like @EmailId  

if (@SessionId is null)
begin
 update uss  set IsActive =0 ,Logout=GETUTCDATE() from [dbo].[UserSessions] uss 
  join UserList us on uss.UserId= us.id  where us.EmailId = @EmailId 
  end
  else 
  begin
  update uss  set IsActive =0 ,Logout=GETUTCDATE() from [dbo].[UserSessions] uss 
  join UserList us on uss.UserId= us.id  where us.EmailId = @EmailId and uss.SessionId=@SessionId
  end
	  
 END      
 GO