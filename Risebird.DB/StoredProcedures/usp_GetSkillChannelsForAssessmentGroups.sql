
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillChannelsForAssessmentGroups'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelsForAssessmentGroups
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetSkillChannelsForAssessmentGroups]           
 
@AssessmentGroupsList varchar(max) = null  
AS            
Begin           
  
  
  
DECLARE @tbl_AssessmentGroups TABLE (          
    --PrimarySkill varchar(200) 
	AGId INT
)   
  
if(@AssessmentGroupsList is not Null and @AssessmentGroupsList != '')  
Begin  
Insert into @tbl_AssessmentGroups  
select value from string_split(@AssessmentGroupsList, ',')  
End  
  
         
    select irf.id, irf.IRFId, ag.AssessmentGroup as AssessmentGroupName from IRF 
	join @tbl_AssessmentGroups tag on tag.AGId = irf.AssessmentGroupId
	join AssessmentGroup ag on ag.id = tag.AGId
    order by ag.AssessmentGroup

End          
   
GO


