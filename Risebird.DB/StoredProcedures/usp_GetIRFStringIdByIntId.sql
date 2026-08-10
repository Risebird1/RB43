IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFStringIdByIntId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFStringIdByIntId
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFStringIdByIntId]
    @id INT,
    @IsDeleted BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IRFId FROM dbo.IRF WHERE ISNULL(IsDeleted, 0) = @IsDeleted AND id = @id;
END
GO
