
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_RemoveVM'))
BEGIN
    DROP PROCEDURE dbo.usp_RemoveVM
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_RemoveVM]
@vmId int

AS
Begin

delete from TransVMUser where VmId=@vmId

delete from ProctorVMQuestions where VmId=@vmId

delete from TransVMTriggerDetails where VMPropertyId = @vmId

delete from VMActionPerformed where VmId = @vmId

delete from VMProperties where id=@vmId

End
GO


