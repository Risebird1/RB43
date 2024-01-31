IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateCampaignSkillChanels'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateCampaignSkillChanels
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_AddUpdateCampaignSkillChanels]    
(    
	@campaignId int,   
	@PerformedBy varchar(100),
	@timeNow datetime,
	@CampaignSkillChannelsBulkData AS [dbo].[CampaignSkillChannels_TableType] READONLY
	
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran  
   
   if not exists(select top 1 id from CampaignSkillChannels where CampaignId = @campaignId)
   Begin

	   insert into CampaignSkillChannels 
		([CampaignId],
		[IRFId],
		[CreatedAt],
		[CreatedBy])
			

		SELECT @campaignId, csbd.IRFId, @timeNow, @PerformedBy FROM @CampaignSkillChannelsBulkData csbd   
		
   End

   else
   Begin
   
   delete from CampaignSkillChannels where CampaignId = @campaignId

   insert into CampaignSkillChannels 
		([CampaignId],
		[IRFId],
		[ModifiedAt],
		[ModifiedBy])
			

		SELECT @campaignId, csbd.IRFId, @timeNow, @PerformedBy FROM @CampaignSkillChannelsBulkData csbd   
		
   End
	

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


