IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InterviewerAvailabilityBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_InterviewerAvailabilityBulkInsert
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_InterviewerAvailabilityBulkInsert]    
(    
      @InterviewerAvailabilityBulkData AS [dbo].[InterviewerAvailability_TableType] READONLY    
)    
AS    
BEGIN    
   
 begin try  
  begin tran  
  
   INSERT INTO [dbo].[InterviewerAvailability](
	[InterviewerProfileId],
	[AvailabilityDate],
	[Hour_00_TO_01],
	[Hour_01_TO_02],
	[Hour_02_TO_03],
	[Hour_03_TO_04],
	[Hour_04_TO_05],
	[Hour_05_TO_06],
	[Hour_06_TO_07],
	[Hour_07_TO_08],
	[Hour_08_TO_09],
	[Hour_09_TO_10],
	[Hour_10_TO_11],
	[Hour_11_TO_12],
	[Hour_12_TO_13],
	[Hour_13_TO_14],
	[Hour_14_TO_15],
	[Hour_15_TO_16],
	[Hour_16_TO_17],
	[Hour_17_TO_18],
	[Hour_18_TO_19],
	[Hour_19_TO_20],
	[Hour_20_TO_21],
	[Hour_21_TO_22],
	[Hour_22_TO_23],
	[Hour_23_TO_00],
	[CreatedBy],
	[CreatedOn],
	[ModifiedBy],
	[ModifiedOn])    
    
   SELECT * FROM @InterviewerAvailabilityBulkData   
  
  commit tran  
 end try  
 begin catch  
  rollback transaction  
  raiserror('Error adding interviewer availability data', 16, 1)  
 end catch  
  
END    
GO


