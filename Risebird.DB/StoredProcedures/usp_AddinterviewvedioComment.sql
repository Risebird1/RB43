/****** Object:  StoredProcedure [dbo].[usp_AddinterviewvedioComment]    Script Date: 4/18/2020 10:56:48 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddinterviewvedioComment'))
BEGIN
    DROP PROCEDURE dbo.usp_AddinterviewvedioComment
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure [dbo].[usp_AddinterviewvedioComment]
@Comment varchar(450),
@VedioId varchar(50),
@EmailID varchar(50)
As
Begin
Declare @id int
Select @id= Id from UserList where EmailId=@EmailID
Insert into RisetubeVideoComment values(@Id,@VedioId, @Comment,GETDATE()) 

END
