
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetApplicableSkillChannelsForApplicantUponEdit'))
BEGIN
    DROP PROCEDURE dbo.usp_GetApplicableSkillChannelsForApplicantUponEdit
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetApplicableSkillChannelsForApplicantUponEdit]            
@applicantId int  
AS            
Begin           
  
declare @AssessmentGroupsList varchar(300)  = (select MAGIds from InterviewerDirectApplication where id = @applicantId)
  
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


