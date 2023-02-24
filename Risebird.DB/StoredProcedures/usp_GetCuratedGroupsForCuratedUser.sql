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

	DECLARE @tbl_TotalSelectedInteviewes TABLE (
		CuratedGroupId INT,
		InterviewesCount int 
	);
	INSERT INTO @tbl_TotalIRFs
	Select cim.id as CuratedGroupId,Count(ic.id) Interviewes from CuratedIRFMapping cim join IRF on IRF.id=cim.IRFId 
	left join InterviewMeetings im on im.IRFId=IRF.IRFId left join InterviewComments ic on ic.InterviewId=im.id
	where ic.MeetingStatus=4 and (ic.InterviewStatus=2 or ic.InterviewStatus=4)
	group by cim.id

	select cg.id as CuratedGroupId, cg.[Name] as CuratedGroupName, cg.IsDeleted,cg.CreatedOn,sub.SubId, tirf.TotalIRFs,tsi.InterviewesCount 
	from CuratedGroup cg
	join CuratedGroupUsers cgu on cgu.CuratedGroupId=cg.id
	left join @tbl_SubscribedTo sub on cg.id=sub.CuratedGroupId
	left join @tbl_TotalIRFs tirf on tirf.CuratedGroupId=cg.id
	left join @tbl_TotalSelectedInteviewes tsi on tsi.CuratedGroupId=cg.id
	where cgu.UserId=@userId

End

GO


