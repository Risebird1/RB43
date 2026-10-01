IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCodeProjectVm'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCodeProjectVm
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetCodeProjectVm
    @Handle varchar(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM CodeProjectVmProvisioning WHERE Handle = @Handle;
END
GO
