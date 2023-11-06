/****** Object:  StoredProcedure [dbo].[usp_AddUpdateZoomOAuthToken]     ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateZoomOAuthToken'))
BEGIN
    DROP PROCEDURE usp_AddUpdateZoomOAuthToken
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_AddUpdateZoomOAuthToken]
@token nvarchar(max),
@currentTime datetime


AS
Begin

IF NOT EXISTS (SELECT 1  FROM  ZoomApiTokenMaster)
Begin

INSERT INTO [dbo].[ZoomApiTokenMaster] VALUES (@token, @currentTime)

End

Else

Begin

Update ZoomApiTokenMaster set Token =@token, Created_ModifiedOn = @currentTime


End

END

GO


