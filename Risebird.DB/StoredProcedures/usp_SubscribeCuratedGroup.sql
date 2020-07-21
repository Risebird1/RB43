/****** Object:  StoredProcedure [dbo].[usp_SubscribeCuratedGroup]    Script Date: 15-07-2020 5.55.44 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SubscribeCuratedGroup'))
BEGIN
    DROP PROCEDURE dbo.usp_SubscribeCuratedGroup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SubscribeCuratedGroup]
@CuratedGroupId int,
@Profile varchar(200),
@EmailId varchar(200)

AS
Begin
declare
@UserId int = (select id from [UserList] where EmailId=@EmailId and idProfile=(select id from [Profile] where [Name]=@Profile))

insert into CuratedGroupSubscribers values (@UserId, @CuratedGroupId)

END

GO


