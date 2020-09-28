/****** Object:  StoredProcedure [dbo].[usp_DeleteTransVmUsers]    Script Date: 28-09-2020 12.03.00 PM ******/
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



