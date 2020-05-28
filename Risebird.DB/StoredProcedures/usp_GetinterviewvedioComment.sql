/****** Object:  StoredProcedure [dbo].[usp_GetinterviewvedioComment]    Script Date: 4/18/2020 10:56:22 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetinterviewvedioComment'))
BEGIN
    DROP PROCEDURE dbo.usp_GetinterviewvedioComment
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure [dbo].[usp_GetinterviewvedioComment]
@VedioId int
As
Begin
Select (ul.FirstName+' '+ul.LastName) as UserName, rc.Comment from RisetubeVideoComment rc
Left join UserList ul on rc.userid=ul.Id
where InterViewId=@VedioId order by Date desc 

END
GO