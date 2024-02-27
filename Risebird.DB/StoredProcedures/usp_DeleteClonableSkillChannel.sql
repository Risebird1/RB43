IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_DeleteClonableSkillChannel'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteClonableSkillChannel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create procedure usp_DeleteClonableSkillChannel
@id int,
@modifiedOn datetime,
@modifyedBy varchar(100)
AS
BEGIN
update ClonableSkillChannel set IsDeleted=1,ModifiedOn=@modifiedOn,ModifiedBy=@modifyedBy where id=@id
END
GO