 
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewShareDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewShareDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
   CREATE Procedure usp_AddInterviewShareDetails  
 @Userid varchar(100),      
 @VedioId int,      
 @Emails varchar(Max)      
 AS      
 BEGIN   
 Declare @id int  
 Select @id=id from userlist where EmailId=@Userid  
 Insert Into InterviewShareDetails  SELECT @VedioId as Interviewid,value as Emailid,Getdate() as SharedOn,@id as SharedBy  FROM STRING_SPLIT(@Emails, ';') as B      
WHERE  NOT EXISTS (SELECT 1       
                   FROM   InterviewShareDetails A      
                   WHERE  A.Emailid = B.value and A.Interviewid=@VedioId)   
 END  
 GO
