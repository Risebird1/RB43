IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddTemplate'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTemplate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  
    
Create PROCEDURE [dbo].[usp_AddTemplate]    
(    
   @TechSkillBulkData AS [dbo].[TransTemplateTechSkill_TableType] READONLY,  
   @InterviewerBulkData AS [dbo].[TransTemplateInterviewer_TableType] READONLY,  
   @TemplateName varchar(100),  
   @AssessmentGroupId int,  
   @createdByEmailId varchar(100)  
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran  
   declare @createdByUserId int, @templateId int  
   set @createdByUserId = (select id from UserList where EmailId =@createdByEmailId)  
  
   insert into SkillChannelTemplate (TemplateName, AssessmentGroupId, CreatedOn, CreatedBy)  
   values (@TemplateName, @AssessmentGroupId, getdate(), @createdByUserId)  
  
  
   --get the id of the template added above  
   set @templateId = SCOPE_IDENTITY()  
  
   -- insert template tech skills  
  
   INSERT INTO [dbo].[TransTemplateTechSkill](    
    [TemplateId] ,    
    [SkillId],    
    [LevelId],  
    [Weightage],  
    [IsSelected])    
     
    
   SELECT @templateId, tsbd.SkillId, tsbd.LevelId, tsbd.Weightage, tsbd.IsSelected FROM @TechSkillBulkData tsbd  
  
  
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


