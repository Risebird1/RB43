IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddIRFInterviewersV2'))
BEGIN
    DROP PROCEDURE dbo.usp_AddIRFInterviewersV2
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_AddIRFInterviewersV2]    
(    
	     
	@timeNow datetime,
	@PerformedBy varchar(100) = null,
	@InterviewerSurchargeBulkData AS [dbo].[TransInterviewerSurcharge_TableTypeV3] READONLY
	
)    
AS    
BEGIN    
  
 begin try  
  begin tran  
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
		[IsSMEVerified],
		[ClientAction],
		[ActivatedAt],
		[AddedAt])  
    
		SELECT distinct ibd.IRFId, ibd.UserId, ibd.PerSuccessRate, ibd.Surcharge,
		CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN 0 ELSE ibd.IsActive END,
		ibd.IsAvailable, InterviewerProfile.id, ibd.[Source],
		null, NULL as IsSurchargeApprove, ibd.IsCampaignActivity, ibd.CampaignId, ibd.OpenAIScreeningResponse, ibd.AIVerdict,
		CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN 0 ELSE 1 END,
		CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN NULL ELSE 'Shortlisted' END,
		CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN NULL ELSE @timeNow END,
		@timeNow
		FROM @InterviewerSurchargeBulkData ibd
		JOIN InterviewerProfile on ibd.UserId = InterviewerProfile.UserId
		JOIN IRF irf ON irf.id = ibd.IRFId
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


