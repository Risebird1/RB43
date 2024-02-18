IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CreateOrUpdateCampaign'))
BEGIN
    DROP PROCEDURE dbo.usp_CreateOrUpdateCampaign
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_CreateOrUpdateCampaign]    
(    
	@campaignId int = null,   
	@AssessmentGroupId int = null,
	@CampaignName varchar(50),        
	@PerformedBy varchar(100),
	@timeNow datetime,
	@CampaignAssessmentGroupsBulkData AS [dbo].[CampaignAssessmentGroups_TableType] READONLY,
	@resultCampaignId int output
	
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran  
   
   if(@campaignId is null or @campaignId = 0)
   Begin
	   if not exists(select top 1 id from AGCampaigns where CampaignName =@CampaignName)
	   Begin

		   insert into AGCampaigns (AssessmentGroupId, CampaignName, CreatedAt, CreatedBy)
		   values (@AssessmentGroupId, @CampaignName, @timeNow, @PerformedBy)


		   select  @resultCampaignId = SCOPE_IDENTITY()
	   

		   insert into CampaignAssessmentGroups 
			([CampaignId],
			[AssessmentGroupId],
			[CreatedAt],
			[CreatedBy])
			

			SELECT @resultCampaignId, cbd.AssessmentGroupId, @timeNow, @PerformedBy FROM @CampaignAssessmentGroupsBulkData cbd   
			


	   End
	   
	   
   End

   else
   Begin
   set @resultCampaignId = @campaignId
   delete from CampaignAssessmentGroups where CampaignId = @campaignId

   insert into CampaignAssessmentGroups 
	([CampaignId],
	[AssessmentGroupId],
	[ModifiedAt],
	[ModifiedBy])
			

	SELECT @resultCampaignId, cbd.AssessmentGroupId, @timeNow, @PerformedBy FROM @CampaignAssessmentGroupsBulkData cbd   
		
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


