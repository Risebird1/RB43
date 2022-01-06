
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteTemplate'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteTemplate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_DeleteTemplate]

@id int

AS
Begin

update SkillChannelTemplate set IsDeleted=1 where id=@id

END

GO


