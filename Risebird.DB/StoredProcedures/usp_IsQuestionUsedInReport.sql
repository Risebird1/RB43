IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_IsQuestionUsedInReport'))
BEGIN
    DROP PROCEDURE [dbo].usp_IsQuestionUsedInReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
    
Create PROCEDURE [dbo].[usp_IsQuestionUsedInReport]    
    
@question varchar(max)    
    
AS    
Begin    
 declare @IsUsed bit  
 set @IsUsed=0  
  
 if exists (select id from TechSkillComments where Comment = @question)  
 begin  
  set @IsUsed=1  
 end  
  
 select @IsUsed  
   
End    
    