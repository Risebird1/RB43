IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CheckExistenceofSkillChannelAgainstRequestId'))
BEGIN
    DROP PROCEDURE dbo.usp_CheckExistenceofSkillChannelAgainstRequestId
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_CheckExistenceofSkillChannelAgainstRequestId]                
      
@requestId nvarchar(200),                
@jobTitle nvarchar(500) = null  
             
AS                
Begin      
    
begin try      
  begin tran      
    declare @irfId int  =0, @assessmentGroupId int = 0, @assessmentGroupName varchar(100), @templateId int = 0, @templateName varchar(200),@IRFName varchar(15)  
    
  if not exists (select 1 from IRF where [RequestId] = @requestId)         
  Begin       
    
        if exists (select 1 from AssessmentGroup ag join MasterAssessmentGroup mag on mag.AssessmentGroupId = ag.id where USTJobTitle = @jobTitle)   
  Begin  
   set @assessmentGroupId = (select ag.id from AssessmentGroup ag join MasterAssessmentGroup mag on mag.AssessmentGroupId = ag.id where USTJobTitle = @jobTitle)   
  End  
          
  End    
  Else  
  Begin  
 set @irfId = (select id from IRF where [RequestId] = @requestId)  
 set @assessmentGroupId =  (select AssessmentGroupId from IRF where id = @irfId)  
  End  
  
  if(@assessmentGroupId > 0)  
  Begin  
 set @assessmentGroupName = (select AssessmentGroup from AssessmentGroup where id = @assessmentGroupId)  
  End  
    
  select top 1 @templateId =csc.id, @templateName = csc.Name from ClonableSkillChannel csc left join irf on irf.id = csc.CreatedFromIRFId where AssessmentGroupId = @assessmentGroupId order by csc.CreatedOn desc  
  Set @IRFName=(Select IRFId from IRF where id= @irfId)
  select @irfId as IRFId, @assessmentGroupId as AssessmentGroupId, @assessmentGroupName as AssessmentGroupName, @templateId as TemplateId, @templateName as TemplateName,@IRFName as IRFName  
      
  commit tran      
 end try      
 begin catch      
      
  rollback transaction      
      
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),      
   @Severity int = ERROR_SEVERITY(),      
   @State smallint = ERROR_STATE()      
       
  RAISERROR (@Message, @Severity, @State)      
       
 end catch    
    
End     
GO


    

