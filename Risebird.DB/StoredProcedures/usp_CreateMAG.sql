IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CreateMAG'))
BEGIN
    DROP PROCEDURE dbo.usp_CreateMAG
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_CreateMAG]    
(    
	@AssessmentGroupId int = null,
	@PerformedBy varchar(100),
	@timeNow datetime,
	@CampaignAssessmentGroupsBulkData AS [dbo].[CampaignAssessmentGroups_TableType] READONLY
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran 
  
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


