IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddEditFitmentReport'))
BEGIN
    DROP PROCEDURE dbo.usp_AddEditFitmentReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

    
Create PROCEDURE [dbo].[usp_AddEditFitmentReport]    
(    
   @JoinabilityReportBulkData AS [dbo].[JoinabilityReport_TableType] READONLY,  
   @SelectabilityReportBulkData AS [dbo].[SelectabilityReport_TableType] READONLY,  
   @FitmentStatus int = null,  
   @PriorityStatus int = null,  
   @CompletionStatus int = null,
   @IRFId int,
   @CandidateId int,
   @JoinabilityScore int,
   @SelectabilityScore int,
   @JoinabilityComments varchar(500) =null,
   @SelectabilityComments varchar(500) = null

)    
AS    
BEGIN    
  
 begin try  
  begin tran  

  --update status' in Candidate table
  update Candidate set FitmentStatus = @FitmentStatus, PriorityStatus =@PriorityStatus, 
  CompletionStatus = @CompletionStatus,
  JoinabilityScore =@JoinabilityScore,
  SelectabilityScore =@SelectabilityScore,
  JoinabilityComments = @JoinabilityComments,
  SelectabilityComments = @SelectabilityComments
  where CandidateMasterId = @CandidateId and IRFId = @IRFId
   
  --delete all from JoinabilityReport where CandidateId = @CandidateId and IRFId = @IRFId
   delete from JoinabilityReport where CandidateId = @CandidateId and IRFId = @IRFId
  
   -- insert into JoinabilityReport  
   INSERT INTO [dbo].[JoinabilityReport](    
    [RequiredLevelId] ,    
    [Type],    
    [ActualLevelId],  
    [WeightageRequired],
	[WeightageActual],
	[Value],
	[CandidateId],
    [IRFId])    

   SELECT * FROM @JoinabilityReportBulkData  
  
  --delete all from SelectabilityReport where CandidateId = @CandidateId and IRFId = @IRFId
  delete from SelectabilityReport where CandidateId = @CandidateId and IRFId = @IRFId
  
   -- insert into SelectabilityReport  
   INSERT INTO [dbo].[SelectabilityReport](    
    [RequiredLevelId] ,    
    [Type],    
    [ActualLevelId],  
    [WeightageRequired],
	[WeightageActual],
	[Value],
	[CandidateId],
    [IRFId])    

   SELECT * FROM @SelectabilityReportBulkData      
    
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


