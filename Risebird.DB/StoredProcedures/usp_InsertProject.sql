/****** Object:  StoredProcedure [dbo].[usp_InsertProject]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertProject'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertProject
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_InsertProject]
@Name varchar(200),
@ClientId int,
@EndClient varchar(200),
@Location varchar(200),
@ProjectOwnerId int,
@IsDeleted bit,
@CreatedOn datetime,
@ProjectUniqueid varchar(50),
@SkillSetsevaluate int,
@ProjectTitle varchar(200),
@NewId int OUTPUT
AS
Begin

INSERT INTO Project (Name, ClientId, EndClient, Location, ProjectOwnerId, IsDeleted, CreatedOn, ProjectUniqueid, SkillSetsevaluate, ProjectTitle)
Values (@Name, @ClientId, @EndClient, @Location, @ProjectOwnerId, @IsDeleted, @CreatedOn, @ProjectUniqueid, @SkillSetsevaluate, @ProjectTitle)

SET @NewId = SCOPE_IDENTITY()

End

GO
