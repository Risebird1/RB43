
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAllMAGs'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllMAGs
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetAllMAGs]     
   
AS    
BEGIN    
--declare @timeNow datetime = dateadd(minute, 330, getdate())
--if(@timeNow > '2024-12-02 16:19:21.007')
--Begin
--	Select ag.id, AssessmentGroup, Isinstaavailable,ISNULL(IsRelevant,0) as IsRelevant 
--	from AssessmentGroup ag join MasterAssessmentGroup mag on mag.AssessmentGroupId = ag.id
--	where mag.IsDeleted = 0 or mag.IsDeleted is null
--End
-- Else
-- Begin
-- Select id,AssessmentGroup,Isinstaavailable,ISNULL(IsRelevant,0) as IsRelevant from AssessmentGroup
-- End

  Select ag.id, AssessmentGroup, Isinstaavailable,ISNULL(IsRelevant,0) as IsRelevant 
	from AssessmentGroup ag join MasterAssessmentGroup mag on mag.AssessmentGroupId = ag.id
	where mag.IsDeleted = 0 or mag.IsDeleted is null  
END
Go

