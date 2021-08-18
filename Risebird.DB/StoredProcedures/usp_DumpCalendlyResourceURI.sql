IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DumpCalendlyResourceURI'))
BEGIN
    DROP PROCEDURE dbo.usp_DumpCalendlyResourceURI
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  
    
Create PROCEDURE [dbo].[usp_DumpCalendlyResourceURI]    
(    
   @SkillChannelCalendlyDetailsBulkData AS [dbo].[SkillChannelCalendlyDetails_TableType] READONLY
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran  
  
  --delete existing irfId

  delete from SkillChannelCalendlyDetails where IRFId in (select IRFId from @SkillChannelCalendlyDetailsBulkData)

   -- insert  
  
   INSERT INTO [dbo].[SkillChannelCalendlyDetails](    
    [IRFId],    
    [ResourceUri],  
    [CreatedOn])    
     
    
   SELECT IRFId, ResourceUri, getdate() FROM @SkillChannelCalendlyDetailsBulkData
    
    
   delete from TransCalendlyLinkAddedUpdated where IRFId in (select IRFId from @SkillChannelCalendlyDetailsBulkData)
  
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


