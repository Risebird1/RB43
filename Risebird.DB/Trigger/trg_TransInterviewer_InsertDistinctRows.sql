--Instead of Insert
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'trg_TransInterviewer_InsertDistinctRows')
BEGIN
    Drop trigger trg_TransInterviewer_InsertDistinctRows
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER trg_TransInterviewer_InsertDistinctRows
ON TransInterviewer
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Insert only distinct rows into the target table
    INSERT INTO [dbo].[TransInterviewer](  
	[IRFId] ,  
	[UserId],  
	[PerSuccessRate], 
	[Surcharge],
	[IsActive],
	[IsAvailable],
	[InterviewerId],
	[IsSurchargeApprove]) 
    SELECT DISTINCT IRFId, UserId, PerSuccessRate, Surcharge, IsActive, IsAvailable, InterviewerId, IsSurchargeApprove
    FROM INSERTED;


END;