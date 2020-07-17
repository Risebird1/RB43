/****** Object:  StoredProcedure [dbo].[usp_GetClientUsers]    Script Date: 16-07-2020 5.01.29 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientUsers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientUsers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClientUsers]

@ClientEmailId varchar(100)

AS
Begin
declare

@clientId int = (select cu.ClientId from UserList ul join ClientUser cu on cu.UserId =ul.id where idProfile = (select id from [Profile] where [Name]='Client') 
				 and EmailId = @ClientEmailId)


select ul.EmailId from ClientUser cu join UserList ul on ul.id = cu.UserId where cu.ClientId=@clientId


End
GO


