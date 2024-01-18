IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateAffindaIndexFlag'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateAffindaIndexFlag
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdateAffindaIndexFlag
@irfId varchar(200),
@isIndexCreated bit
AS
BEGIN

Update IRF set IsAffindaIndexCreated=@isIndexCreated where IRFId=@irfId

END
Go