
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_RestoreTemplate'))
BEGIN
    DROP PROCEDURE dbo.usp_RestoreTemplate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_RestoreTemplate]

@id int

AS
Begin

update SkillChannelTemplate set IsDeleted=0 where id=@id

END

GO


