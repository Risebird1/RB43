IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMAGSecondaryAssessmentGroupsById'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetMAGSecondaryAssessmentGroupsById]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetMAGSecondaryAssessmentGroupsById]               
@assessmentGroupId int    
AS                
BEGIN 

select ag.AssessmentGroup as AssessmentGroupNameForMAG, sag.AssessmentGroupId, agSec.AssessmentGroup as SecondaryAssessmentGroupName  from SecondaryAssessmentGroups sag
join MasterAssessmentGroup MAG on MAG.id = sag.MAGId
join AssessmentGroup ag on ag.id = mag.AssessmentGroupId
left join AssessmentGroup agSec on agSec.id = sag.AssessmentGroupId
where mag.AssessmentGroupId = @assessmentGroupId

END


GO

    
      