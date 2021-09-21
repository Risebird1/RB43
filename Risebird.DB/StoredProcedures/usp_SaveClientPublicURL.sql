IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveClientPublicURL'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveClientPublicURL
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

    
    
Create PROCEDURE [dbo].[usp_SaveClientPublicURL]      
      
@emailId varchar(100),  
@uniqueid varchar(100)  
      
AS      
Begin      

If Not Exists (Select 1 from ClientPublicURLs where Emailid=@emailId)    
Begin
Insert into ClientPublicURLs(uniqueid,Emailid) values(@uniqueid,@emailId)  
End
Select uniqueid from ClientPublicURLs where Emailid=@emailId
End 
GO