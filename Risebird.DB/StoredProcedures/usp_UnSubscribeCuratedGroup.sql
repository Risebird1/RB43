/****** Object:  StoredProcedure [dbo].[usp_UnSubscribeCuratedGroup]    Script Date: 15-07-2020 5.56.04 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UnSubscribeCuratedGroup'))
BEGIN
    DROP PROCEDURE dbo.usp_UnSubscribeCuratedGroup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_UnSubscribeCuratedGroup]
@CuratedGroupId int,
@Profile varchar(200),
@EmailId varchar(200)

AS
Begin
declare
@UserId int = (select id from [UserList] where EmailId=@EmailId and idProfile=(select id from [Profile] where [Name]=@Profile))

delete from CuratedGroupSubscribers where UserId=@UserId and CuratedGroupId=@CuratedGroupId

END

GO


