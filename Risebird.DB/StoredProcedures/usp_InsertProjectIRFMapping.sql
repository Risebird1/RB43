/****** Object:  StoredProcedure [dbo].[usp_InsertProjectIRFMapping]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertProjectIRFMapping'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertProjectIRFMapping
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_InsertProjectIRFMapping]
@ProjectId int,
@IRFId int,
@IsAdded bit
AS
Begin

INSERT INTO ProjectIRFMapping (ProjectId, IRFId, IsAdded)
Values (@ProjectId, @IRFId, @IsAdded)

End

GO
