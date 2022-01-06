
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTemplateInterviewers'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetTemplateInterviewers]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetTemplateInterviewers]

@templateId int

AS
Begin

select tti.*, ipr.UserId, dbo.fn_GetInterviewerPrimarySkillsCommaSeparated(tti.InterviewerId) as PrimarySkill
from TransTemplateInterviewer tti
join InterviewerProfile ipr on ipr.id = tti.InterviewerId
where TemplateId=@templateId

End



GO

