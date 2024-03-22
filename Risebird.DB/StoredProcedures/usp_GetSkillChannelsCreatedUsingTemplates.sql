
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillChannelsCreatedUsingTemplates'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetSkillChannelsCreatedUsingTemplates]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetSkillChannelsCreatedUsingTemplates]


AS
Begin

WITH CTE AS (
        SELECT Particulars, PerformedBy
        FROM ActivityTracker
        WHERE ActivityType=2--activity type 2 corresponds to creation of skill channel(refer enum)
)

select irf.IRFId, irf.CreatedOn, csc.[Name] as CreatedFromTemplateName, cl.[Name] as ClientCompName, 
ul.EmailId as RisebirdHREmail, cte.PerformedBy as CreatedBy, asg.AssessmentGroup as AssessmentGroupName
from IRF 
join ClonableSkillChannel csc on csc.id = irf.CreatedFromTemplateId
join Client cl on cl.id = irf.ClientId
join CTE on cte.Particulars = irf.IRFId
join UserList ul on ul.id = irf.RisebirdHr
join AssessmentGroup asg on asg.id = irf.AssessmentGroupId
where CreatedFromTemplateId is not null and CreatedFromTemplateId > 0 
order by irf.CreatedOn desc

End



GO

