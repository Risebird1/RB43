  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddAlertMessage'))
BEGIN
    DROP PROCEDURE dbo.usp_AddAlertMessage
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
         
CREATE Procedure usp_AddAlertMessage  
@ID int,  
@Announcement varchar(max),                
@StartTime datetime,          
@EndTime datetime,       
@Isactive bit        
AS                
BEGIN            
if(@ID=0)          
Begin          
Insert into alertmessage(Announcement,StartTime,EndTime,IsActive)     
values(@Announcement,@StartTime,@EndTime,@Isactive)          
END          
else          
Begin          
Update alertmessage set Announcement=@Announcement,StartTime=@StartTime,EndTime=@EndTime,IsActive=@Isactive   
where ID=@ID         
end          
END       
Go
      