IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddCodeProjectVMProperty'))
BEGIN
    DROP PROCEDURE dbo.usp_AddCodeProjectVMProperty
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_AddCodeProjectVMProperty
    @Name varchar(150),
    @ResourceGroupName varchar(150),
    @OSType varchar(25) = NULL,
    @VmLocation varchar(150) = NULL,
    @UserName varchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- No password is stored here: candidate credentials live encrypted in CodeProjectVmProvisioning.
    INSERT INTO VMProperties (Name, ResourceGroupName, Status, IsAdded, IsRemoved, IsDeleted, OSType, VmLocation, UserName, CreateDate, Purpose)
    VALUES (@Name, @ResourceGroupName, 6, 1, 0, 0, @OSType, @VmLocation, @UserName, GETDATE(), 'CodeProject');
    SELECT CAST(SCOPE_IDENTITY() AS int) AS NewId;
END
GO
