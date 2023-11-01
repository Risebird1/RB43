/****** Object:  StoredProcedure [dbo].[usp_GetZoomOAuthToken]    ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetZoomOAuthToken'))
BEGIN
    DROP PROCEDURE usp_GetZoomOAuthToken
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetZoomOAuthToken]

AS
Begin

select * from ZoomApiTokenMaster

END

GO


