IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateDmIvPolicy'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateDmIvPolicy
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateDmIvPolicy]
    @irfId int,
    @dmIvLockNew bit,
    @dmAssignmentPolicy varchar(20),
    @dmDefaultInterviewCap int,
    @performedBy varchar(75) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE IRF
    SET DmIvLockNew = @dmIvLockNew,
        DmAssignmentPolicy = @dmAssignmentPolicy,
        DmDefaultInterviewCap = @dmDefaultInterviewCap,
        ModifiedOn = GETDATE()
    WHERE id = @irfId;
END
GO
