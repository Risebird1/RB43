/****** Object:  StoredProcedure [dbo].[usp_SubscribeIRF]    Script Date: 01-05-2020 1.45.19 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SubscribeIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_SubscribeIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create PROCEDURE [dbo].[usp_SubscribeIRF]
@IRFId int,
@Profile varchar(200),
@EmailId varchar(200)

AS
Begin
declare
@UserId int = (select id from [UserList] where EmailId=@EmailId and idProfile=(select id from [Profile] where [Name]=@Profile))

insert into Subscribers values (@UserId, @IRFId)

END

GO


