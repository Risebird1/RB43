/****** Object:  StoredProcedure [dbo].[usp_GetCuratedGroupUsers]    Script Date: 13-07-2020 10.44.52 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCuratedGroupUsers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCuratedGroupUsers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetCuratedGroupUsers]
@CuratedGroupId int

AS
Begin

declare
@ProfileId int = (select id from [Profile] where [Name]='CuratedUser')

IF NOT EXISTS (SELECT 1 FROM CuratedGroupUsers  WHERE  CuratedGroupId = @CuratedGroupId)
Begin
 
	select id as UserId, EmailId, @CuratedGroupId as CuratedGroupId,
	(select [Name] from CuratedGroup where id =@CuratedGroupId) as CuratedGroupName
	from UserList where idProfile = @ProfileId

End

Else

Begin

	select ul.id as UserId, ul.EmailId, @CuratedGroupId as CuratedGroupId, 
	(select [Name] from CuratedGroup where id =@CuratedGroupId) as CuratedGroupName,
	cgu.IsAdded from UserList ul 
	left join (select * from CuratedGroupUsers where CuratedGroupId = @CuratedGroupId) cgu on cgu.UserId=ul.id
	where ul.idProfile=@ProfileId

End

END
GO


