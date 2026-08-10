/****** Object:  StoredProcedure [dbo].[usp_InsertAccountProjectMapping]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertAccountProjectMapping'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertAccountProjectMapping
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_InsertAccountProjectMapping]
@AccountId int,
@ProjectId int,
@IsAdded bit
AS
Begin

INSERT INTO AccountProjectMapping (AccountId, ProjectId, IsAdded)
Values (@AccountId, @ProjectId, @IsAdded)

End

GO
