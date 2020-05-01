/****** Object:  StoredProcedure [dbo].[usp_UnSubscribeIRF]    Script Date: 01-05-2020 1.45.11 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UnSubscribeIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_UnSubscribeIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_UnSubscribeIRF]
@IRFId int,
@Profile varchar(200),
@EmailId varchar(200)

AS
Begin
declare
@UserId int = (select id from [UserList] where EmailId=@EmailId and idProfile=(select id from [Profile] where [Name]=@Profile))

delete from Subscribers where UserId=@UserId and IRFId=@IRFId

END

GO


