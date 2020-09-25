/****** Object:  StoredProcedure [dbo].[usp_DeleteTransVmUsers]    Script Date: 25-09-2020 11.57.57 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteTransVmUsers'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteTransVmUsers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_DeleteTransVmUsers]

@vmId int

AS
Begin

delete from TransVMUser where VmId=@vmId

End
GO


