/****** Object:  StoredProcedure [dbo].[usp_GetUserId]    Script Date: 08-01-2020 7.40.41 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetQuestionSkillById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetQuestionSkillById
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetQuestionSkillById]
--@techSkillCommentId int,
@questionText varchar(max),
@irfId int,
@emailId varchar(150)

AS
Begin

	--select SkillName from TechSkillComments where id = @techSkillCommentId
	declare @userId int, @profileId int

	set @userId = (select id from UserList where EmailId = @emailId)
	set @profileId = (select idProfile from UserList where id = @userId)
	if(@profileId = 1 or @profileId = 32)
	begin
		select top 1 SkillName from TechSkillComments where irfid = @irfId and Comment like @questionText
		order by id desc
	end
	else
	begin 
		select top 1 SkillName from TechSkillComments where irfid = @irfId and Comment like @questionText and UserId = @userId
		order by id desc
	end

	

END

GO