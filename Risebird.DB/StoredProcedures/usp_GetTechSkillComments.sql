IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTechSkillComments'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTechSkillComments
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetTechSkillComments]
@IRFId int,   
@emailId varchar(100)


AS
Begin
		
if(@emailId is not null)
begin
	declare @profile int
	set @profile = (select idProfile from UserList where EmailId like @emailId)

	if(@profile=1)--Admin
	begin
		Select tsc.*, sk.[Name] as SkillName from TechSkillComments tsc 
		join Skill sk on sk.id = tsc.Skill where IRFId = @IRFId
	end
	else
	begin 
		declare @userId int
		set @userId = (select id from UserList where EmailId like @emailId)

		Select tsc.*, sk.[Name] as SkillName from TechSkillComments tsc 
		join Skill sk on sk.id = tsc.Skill where IRFId = @IRFId and UserId = @userId
	end
end
else
begin
	Select tsc.*, sk.[Name] as SkillName from TechSkillComments tsc 
	join Skill sk on sk.id = tsc.Skill where IRFId = @IRFId
end

END

GO


