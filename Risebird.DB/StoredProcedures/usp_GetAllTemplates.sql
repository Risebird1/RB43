IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllTemplates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllTemplates
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllTemplates]  
@emailId varchar(100)
--@startDate datetime,
--@endDate datetime
AS  
Begin 

declare  @emailIdParam varchar(100), @profile varchar(100)--, @startDateParam datetime,  @endDateParam datetime,
--set @startDateParam = @startDate
--set @endDateParam = @endDate
set @emailIdParam = @emailId


select @profile = [Profile].[Name] from UserList ul join [Profile] on [Profile].id = ul.idProfile where EmailId = @emailIdParam 


if(@profile='Admin')
Begin
	select sct.*, ul.EmailId as CreatedByEmailId, ag.AssessmentGroup as AssessmentGroupName from SkillChannelTemplate sct 
	join UserList ul on ul.id = sct.CreatedBy
	join AssessmentGroup ag on ag.id = sct.AssessmentGroupId
	order by CreatedOn desc --where  IsDeleted = 0 and  CreatedOn>@startDateParam and CreatedOn<@endDateParam 
End
else
Begin
	select sct.*, ul.EmailId as CreatedByEmailId, ag.AssessmentGroup as AssessmentGroupName from SkillChannelTemplate sct 
	join UserList ul on ul.id = sct.CreatedBy
	join AssessmentGroup ag on ag.id = sct.AssessmentGroupId
	where ul.EmailId = @emailIdParam order by CreatedOn desc -- IsDeleted = 0 and  CreatedOn>@startDateParam and CreatedOn<@endDateParam and
End

End
GO


