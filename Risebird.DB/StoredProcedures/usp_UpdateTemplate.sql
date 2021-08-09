IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateTemplate'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateTemplate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  
    
Create PROCEDURE [dbo].[usp_UpdateTemplate]    
(    
   @TechSkillBulkData AS [dbo].[TransTemplateTechSkill_TableType] READONLY,  
   @InterviewerBulkData AS [dbo].[TransTemplateInterviewer_TableType] READONLY,  
   @AssessmentGroupId int,  
   @modifiedByEmailId varchar(100),  
   @templateId int
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran  

   declare @modifiedByUserId int
   set @modifiedByUserId = (select id from UserList where EmailId = @modifiedByEmailId)  
  
   update SkillChannelTemplate set
   AssessmentGroupId =@AssessmentGroupId,
   ModifiedOn = getdate(),
   ModifiedBy =@modifiedByUserId
   where id = @templateId
  
  --delete all tedch skills from TransTemplateTechSkill for the template
   delete from TransTemplateTechSkill where TemplateId = @templateId
  
   -- insert template tech skills  
   INSERT INTO [dbo].[TransTemplateTechSkill](    
    [TemplateId] ,    
    [SkillId],    
    [LevelId],  
    [Weightage],  
    [IsSelected])    
     
   SELECT @templateId, tsbd.SkillId, tsbd.LevelId, tsbd.Weightage, tsbd.IsSelected FROM @TechSkillBulkData tsbd  
  
  
  --delete all interviewers from TransTemplateInterviewer for the template
  delete from TransTemplateInterviewer where TemplateId = @templateId
   
   --insert template interviewers   
   INSERT INTO [dbo].[TransTemplateInterviewer](    
    [TemplateId],    
    [InterviewerId],  
    [IsSelected])    
     
   SELECT @templateId, ipr.Id, ibd.IsSelected FROM @InterviewerBulkData ibd   
   join InterviewerProfile ipr on ipr.UserId= ibd.UserId    
    
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


