/****** Object:  StoredProcedure [dbo].[usp_GetAllTriggeredVMs]    Script Date: 16-09-2020 6.56.42 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllTriggeredVMs'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllTriggeredVMs
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllTriggeredVMs]

AS
Begin

select tvm.Id, tvm.LastAction, tvm.ExpectedStatus, tvm.VMPropertyId, vmp.[Name], vmp.ResourceGroupName, vmp.[Status] from TransVMTriggerDetails tvm join VMProperties vmp on vmp.id = tvm.VMPropertyId

End

GO


