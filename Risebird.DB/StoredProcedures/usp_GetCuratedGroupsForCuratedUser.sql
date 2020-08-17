/****** Object:  StoredProcedure [dbo].[usp_GetCuratedGroupsForCuratedUser]    Script Date: 15-07-2020 5.55.25 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCuratedGroupsForCuratedUser'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCuratedGroupsForCuratedUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetCuratedGroupsForCuratedUser]  
@EmailId varchar(200)  
AS  
Begin 

	declare @userId int
	set @userId =(select id from UserList where EmailId=@EmailId)

	DECLARE @tbl_TotalIRFs TABLE (
		CuratedGroupId INT,
		TotalIRFs int 
	);
	INSERT INTO @tbl_TotalIRFs
	select CuratedGroupId, Count(CuratedGroupId) as TotalIRFs from CuratedIRFMapping group by CuratedGroupId

	DECLARE @tbl_SubscribedTo TABLE (
		SubId int,
		CuratedGroupId INT
	);
	INSERT INTO @tbl_SubscribedTo
	select id, CuratedGroupId from CuratedGroupSubscribers
	where UserId= @userId

	select cg.id as CuratedGroupId, cg.[Name] as CuratedGroupName, cg.IsDeleted,cg.CreatedOn,sub.SubId, tirf.TotalIRFs from CuratedGroup cg
	join CuratedGroupUsers cgu on cgu.CuratedGroupId=cg.id
	left join @tbl_SubscribedTo sub on cg.id=sub.CuratedGroupId
	left join @tbl_TotalIRFs tirf on tirf.CuratedGroupId=cg.id
	where cgu.UserId=@userId

End

GO


