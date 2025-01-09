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
	@CreateMasterAG bit,
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
			
			if(@CreateMasterAG = 1)
			Begin

			declare @resultMagId int

			insert into MasterAssessmentGroup(MAGName, AssessmentGroupId, CreatedOn, CreatedBy)
			values ((select AssessmentGroup from AssessmentGroup where id = @AssessmentGroupId), @AssessmentGroupId, @timeNow, @PerformedBy)

			select  @resultMagId = SCOPE_IDENTITY()

			insert into SecondaryAssessmentGroups 
			([MAGId],
			[AssessmentGroupId],
			[CreatedModifiedOn])

			SELECT @resultMagId, cbd.AssessmentGroupId, @timeNow FROM @CampaignAssessmentGroupsBulkData cbd   
			WHERE NOT EXISTS (
			SELECT 1
			FROM SecondaryAssessmentGroups
			WHERE SecondaryAssessmentGroups.AssessmentGroupId = cbd.AssessmentGroupId
			  AND SecondaryAssessmentGroups.MAGId = @resultMagId
			  --and cbd.AssessmentGroupId != @AssessmentGroupId
			) and cbd.AssessmentGroupId != @AssessmentGroupId
			and ((select id from MasterAssessmentGroup where AssessmentGroupId = cbd.AssessmentGroupId) is null)

			End

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
	
	if(@CreateMasterAG = 1)
	Begin

	declare @magId int
	declare @primaryAGId int = (select AssessmentGroupId from AGCampaigns where id =@campaignId)
			
	select  @magId = id from MasterAssessmentGroup where AssessmentGroupId = @primaryAGId

	delete from SecondaryAssessmentGroups where MAGId= @magId

	insert into SecondaryAssessmentGroups 
	([MAGId],
	[AssessmentGroupId],
	[CreatedModifiedOn])

	SELECT @magId, cbd.AssessmentGroupId, @timeNow FROM @CampaignAssessmentGroupsBulkData cbd   
	WHERE NOT EXISTS (
	SELECT 1
	FROM SecondaryAssessmentGroups
	WHERE SecondaryAssessmentGroups.AssessmentGroupId = cbd.AssessmentGroupId
		AND SecondaryAssessmentGroups.MAGId = @magId
		--and cbd.AssessmentGroupId != @primaryAGId
	) and cbd.AssessmentGroupId != @primaryAGId
	and ((select id from MasterAssessmentGroup where AssessmentGroupId = cbd.AssessmentGroupId) is null)

	END

	

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


