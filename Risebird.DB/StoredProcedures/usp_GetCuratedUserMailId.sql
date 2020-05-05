/****** Object:  StoredProcedure [dbo].[usp_GetCuratedUserMailId]    Script Date: 05-05-2020 7.25.43 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCuratedUserMailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCuratedUserMailId
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetCuratedUserMailId]
@irfId int

AS
Begin

select ul.EmailId from Subscribers sub join UserList ul on sub.UserId=ul.id where sub.IRFId=@irfId

END

GO


