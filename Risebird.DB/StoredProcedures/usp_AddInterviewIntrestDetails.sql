 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewIntrestDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddInterviewIntrestDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 Create Procedure usp_AddInterviewIntrestDetails  
 @videoid int,  
 @UrlType varchar(10),  
 @Emailid varchar(100)  
 AS  
 BEGIN  
 Declare @id int  
 Select @id=id from UserList where Emailid=@Emailid  
 Insert into InterviewIntrestDetails values(@videoid,@UrlType,@id,Getdate())  
 END