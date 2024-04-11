IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_EditMAG'))
BEGIN
    DROP PROCEDURE dbo.usp_EditMAG
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_EditMAG]    
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
  
	declare @MagId int
    
	set  @MagId = (select id from MasterAssessmentGroup where AssessmentGroupId = @AssessmentGroupId)

	update MasterAssessmentGroup set ModifiedBy =@PerformedBy, ModifiedOn = @timeNow where id = @MagId

	if(@MagId is not null and @MagId > 0)
	Begin
		insert into SecondaryAssessmentGroups 
		([MAGId],
		[AssessmentGroupId],
		[CreatedModifiedOn])

		SELECT @MagId, cbd.AssessmentGroupId, @timeNow FROM @CampaignAssessmentGroupsBulkData cbd   
		WHERE NOT EXISTS (
		SELECT 1
		FROM SecondaryAssessmentGroups
		WHERE SecondaryAssessmentGroups.AssessmentGroupId = cbd.AssessmentGroupId
			AND SecondaryAssessmentGroups.MAGId = @MagId
		) and cbd.AssessmentGroupId != @AssessmentGroupId
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


