/****** Object:  StoredProcedure [dbo].[usp_GetUserId]    Script Date: 08-01-2020 7.40.41 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTechSkillQuestionsByUserId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTechSkillQuestionsByUserId
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTechSkillQuestionsByUserId]
@IRFId int,
@UserId int

AS
Begin

if(@userId != 0)
Begin
	declare @profile varchar(20)

	select @profile = pl.Name from UserList ul join Profile pl on pl.id = ul.idProfile where ul.id= @UserId

	if(@profile = 'Admin')
	begin
		select distinct Comment, SkillName from TechSkillComments tsc where IRFId = @IRFId
	end

	else
	begin
		select distinct Comment, SkillName from TechSkillComments tsc where IRFId = @IRFId and (UserId is null or UserId=@UserId)
	end
End

else
begin
	select distinct Comment, SkillName from TechSkillComments tsc where IRFId = @IRFId
end

END

GO


