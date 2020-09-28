/****** Object:  StoredProcedure [dbo].[usp_UpdateVMStatus]    Script Date: 16-09-2020 6.59.02 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateVMStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateVMStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateVMStatus]
@id int,
@status int


AS
Begin

Update VMProperties set [Status]=@status where id = @id

End

GO


