IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_TransInterviewer_DeleteDuplicateRows')
BEGIN
    Drop trigger tr_TransInterviewer_DeleteDuplicateRows
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
Create TRIGGER tr_TransInterviewer_DeleteDuplicateRows  
ON TransInterviewer  
After INSERT  
AS  
BEGIN  
    SET NOCOUNT ON;  
  Declare @IRFId int
Set @IRFId=(Select top 1 IRFId from inserted)
  
 INSERT INTO [dbo].[TransInterviewer_Copy]  
           ([IRFId]  
           ,[InterviewerId]  
           ,[UserId]  
           ,[PerSuccessRate]  
           ,[IsActive]  
           ,[ClientAction]  
           ,[IsAvailable]  
           ,[IsSMEVerified]  
           ,[Surcharge]  
           ,[Source]  
           ,[ModifiedBy]  
           ,[ActivatedAt]  
           ,[CampaignId]  
           ,[IsCampaignActivity]  
           ,[IsSurchargeApprove]  
           ,[OpenAIScreeningResponse]  
           ,[AIVerdict]  
           ,[IsAIScreeningAtempted]  
           ,[AddedAt]  
           ,[ModifiedAt])   
    SELECT IRFId, InterviewerId, UserId, PerSuccessRate, IsActive, ClientAction, IsAvailable,  
 IsSMEVerified, Surcharge, [Source], ModifiedBy, ActivatedAt, CampaignId, IsCampaignActivity, IsSurchargeApprove,  
 OpenAIScreeningResponse, AIVerdict, IsAIScreeningAtempted, AddedAt, ModifiedAt  
 IsSurchargeApprove  
    FROM INSERTED WHERE   
 NOT EXISTS (  
     SELECT 1  
     FROM [dbo].[TransInterviewer] ti  
     WHERE ti.UserId = inserted.UserId AND ti.IRFId = inserted.IRFId  
 );  

-- Delete Duplicate Interviewer from SkillChannel   
Delete from  TransInterviewer 
WHERE id NOT IN (
    SELECT min(id)
    FROM TransInterviewer 
	where IRFId=@IRFId
    GROUP BY IRFId,UserId
)  and IRFId=@IRFId
  
END
GO