IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddIRFInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_AddIRFInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_AddIRFInterviewers]    
(    
	@id int,        
	@timeNow datetime,
	@IsNewSkillChannel bit = null,
	@PerformedBy varchar(100) = null,
	@InterviewerBulkData AS [dbo].[TransInterviewer_TableTypeV3] READONLY,--[dbo].[TransInterviewer_TableType]

	@InterviewerSurchargeBulkData AS [dbo].[TransInterviewerSurcharge_TableTypeV3] READONLY --[dbo].[TransInterviewerSurcharge_TableType]
	
)    
AS    
BEGIN    
  
 begin try  
  begin tran  
   
	 if(@IsNewSkillChannel is not null and @IsNewSkillChannel = 1)
	 Begin
		INSERT INTO [dbo].[TransInterviewer](  
		[IRFId] ,  
		[UserId],  
		[PerSuccessRate], 
		[Surcharge],
		[IsActive],
		[IsAvailable],
		[InterviewerId],
		[Source],
		[ModifiedBy],
		[IsSurchargeApprove],
		[IsCampaignActivity],
		[CampaignId],
		[OpenAIScreeningResponse],
		[AIVerdict],
		[AddedAt])  
    
		SELECT ibd.IRFId, ibd.UserId, ibd.PerSuccessRate, ibd.Surcharge, ibd.IsActive, ibd.IsAvailable, InterviewerProfile.id, ibd.[Source],
		null, NULL as IsSurchargeApprove, ibd.IsCampaignActivity, ibd.CampaignId, ibd.OpenAIScreeningResponse, ibd.AIVerdict, @timeNow
		FROM @InterviewerSurchargeBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId  
		WHERE 
		NOT EXISTS (
			SELECT 1
			FROM [dbo].[TransInterviewer] ti
			WHERE ti.UserId = ibd.UserId AND ti.IRFId = ibd.IRFId
		);
		
		Insert into SurchargeActivityTracker(InterviewId,Surcharge,SurchargeComment,RequestBy,RequestAt,RequestType,IRFId,UserId)
		SELECT 
		0 AS InterviewId,
		ibd.Surcharge,
		NULL AS SurchargeComment,
		@PerformedBy,
		@timeNow,
		'Raised' AS RequestType,
		ibd.IRFId AS IRFId,
		ibd.UserId
	FROM 
		@InterviewerSurchargeBulkData ibd where ISNULL(ibd.Surcharge,0)<>0;

	 End
	 else
	 Begin
		INSERT INTO [dbo].[TransInterviewer](  
		[IRFId] ,  
		[UserId],  
		[PerSuccessRate], 
		[IsActive],
		[IsAvailable],
		[InterviewerId],
		[Source],
		[ModifiedBy],
		[IsCampaignActivity],
		[CampaignId],
		[OpenAIScreeningResponse],
		[AIVerdict],
		[IsSurchargeApprove],
		[AddedAt])  
    
		SELECT ibd.IRFId, ibd.UserId, ibd.PerSuccessRate, ibd.IsActive, ibd.IsAvailable, InterviewerProfile.id as InterviewerId,
		ibd.[Source], @PerformedBy, ibd.IsCampaignActivity, ibd.CampaignId, ibd.OpenAIScreeningResponse, ibd.AIVerdict, 
		ibd.IsSurchargeApprove, @timeNow
		FROM @InterviewerBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId  
		WHERE 
		NOT EXISTS (
			SELECT 1
			FROM [dbo].[TransInterviewer] ti
			WHERE ti.UserId = ibd.UserId AND ti.IRFId = ibd.IRFId
		);
  
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


