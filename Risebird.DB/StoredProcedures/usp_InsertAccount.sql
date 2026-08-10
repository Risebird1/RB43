/****** Object:  StoredProcedure [dbo].[usp_InsertAccount]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertAccount'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertAccount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_InsertAccount]
@Name varchar(200),
@ClientId int,
@EndClient varchar(200),
@Location varchar(200),
@AccountOwnerId int,
@IsDeleted bit,
@CreatedOn datetime,
@NewId int OUTPUT
AS
Begin

INSERT INTO Account (Name, ClientId, EndClient, Location, AccountOwnerId, IsDeleted, CreatedOn)
Values (@Name, @ClientId, @EndClient, @Location, @AccountOwnerId, @IsDeleted, @CreatedOn)

SET @NewId = SCOPE_IDENTITY()

End

GO
