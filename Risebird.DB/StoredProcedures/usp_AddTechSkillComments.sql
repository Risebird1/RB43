/****** Object:  StoredProcedure [dbo].[usp_AddTechSkillComments]    Script Date: 12/22/2021 5:45:42 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddTechSkillComments'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTechSkillComments
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  

Create PROCEDURE [dbo].[usp_AddTechSkillComments]    
(    
   @TechSkillCommentsBulkData AS [dbo].[TechSkillComments_TableType] READONLY,  
   @IRFId int,
   @userId int
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran 
  
  declare @profileId int
  set @profileId = (select idProfile from UserList where id = @userId)
  if(@profileId = 1)--Admin
  begin
	delete from TechSkillComments where IRFId =@IRFId

   INSERT INTO [dbo].[TechSkillComments](    
    [IRFId] ,    
    [Skill],    
    [Comment],  
    [UserId])    
     
    
   SELECT tscbd.IRFId, tscbd.Skill, tscbd.Comment, tscbd.UserId FROM @TechSkillCommentsBulkData tscbd

  end
  else
  begin
	delete from TechSkillComments where IRFId =@IRFId and UserId =@userId

   INSERT INTO [dbo].[TechSkillComments](    
    [IRFId] ,    
    [Skill],    
    [Comment],  
    [UserId])    
     
    
   SELECT tscbd.IRFId, tscbd.Skill, tscbd.Comment, tscbd.UserId FROM @TechSkillCommentsBulkData tscbd
  end
     
  
  
  commit tran  
 end try  
 begin catch  
  
  rollback transaction  
  
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),  
   @Severity int = ERROR_SEVERITY(),  
   @State smallint = ERROR_STATE()  
   
  RAISERROR (@Message, @Severity, @State)  
   
 end catch  
  
    
END    
GO


