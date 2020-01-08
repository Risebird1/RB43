/****** Object:  StoredProcedure [dbo].[usp_GetUserId]    Script Date: 08-01-2020 7.40.41 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUserId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUserId
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetUserId]
@EmailId varchar(200),
@Profile varchar(200)

AS
Begin
declare
@ProfileId int = (select id from [Profile] where [Name] = @Profile)

select id from UserList where EmailId=@EmailId and idProfile=@ProfileId

END

GO


