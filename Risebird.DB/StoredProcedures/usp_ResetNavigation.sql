/****** Object:  StoredProcedure [dbo].[usp_ResetNavigation]    Script Date: 23-10-2020 11.49.18 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ResetNavigation'))
BEGIN
    DROP PROCEDURE dbo.usp_ResetNavigation
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_ResetNavigation] 
@userId int 
   

AS
BEGIN

update UserList set LastLogin = null, LastLogout=null where id=@userId
 
END
GO



