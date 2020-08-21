 
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewShareDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewShareDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
   CREATE Procedure usp_AddInterviewShareDetails  
 @isShared varchar(10),  
 @VedioId int,  
 @Emails varchar(Max)  
 AS  
 BEGIN  
 Insert Into InterviewShareDetails  SELECT @VedioId as Interviewid,value as Emailid,Getdate() as SharedOn  FROM STRING_SPLIT(@Emails, ';') as B  
WHERE  NOT EXISTS (SELECT 1   
                   FROM   InterviewShareDetails A  
                   WHERE  A.Emailid = B.value and A.Interviewid=@VedioId)  
 END  

