/****** Object:  StoredProcedure [dbo].[usp_GetAllVMs]    Script Date: 16-09-2020 6.55.55 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllVMs'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllVMs
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllVMs]



AS
Begin

select vmp.*, tvm.LastAction from VMProperties vmp left join TransVMTriggerDetails tvm  on tvm.VMPropertyId=vmp.id  where IsRemoved=0 and IsDeleted=0 and ISNULL(vmp.Purpose, '') <> 'CodeProject'


End

GO


