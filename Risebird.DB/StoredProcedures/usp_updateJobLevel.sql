IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_updateJobLevel'))
BEGIN
    DROP PROCEDURE dbo.usp_updateJobLevel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_updateJobLevel
@irfid int,
@joblevel int
AS
BEGIN
Update IRF set JobLevel=@joblevel where id=@irfid
END
Go