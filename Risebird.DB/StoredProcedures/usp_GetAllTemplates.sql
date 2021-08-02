IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllTemplates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllTemplates
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllTemplates]  
@emailId varchar(100),
@startDate datetime,
@endDate datetime
AS  
Begin 

declare @startDateParam datetime,  @endDateParam datetime, @emailIdParam varchar(100), @profile varchar(100)
set @startDateParam = @startDate
set @endDateParam = @endDate
set @emailIdParam = @emailId


select @profile = [Profile].[Name] from UserList ul join [Profile] on [Profile].id = ul.idProfile where EmailId = @emailIdParam 


if(@profile='Admin')
Begin
	select sct.*, ul.EmailId as CreatedByEmailId from SkillChannelTemplate sct join UserList ul on ul.id = sct.CreatedBy
	where IsDeleted = 0 and  CreatedOn>@startDateParam and CreatedOn<@endDateParam order by CreatedOn desc
End
else
Begin
	select sct.*, ul.EmailId as CreatedByEmailId from SkillChannelTemplate sct join UserList ul on ul.id = sct.CreatedBy
	where IsDeleted = 0 and  CreatedOn>@startDateParam and CreatedOn<@endDateParam and ul.EmailId = @emailIdParam order by CreatedOn desc 
End

End
GO


