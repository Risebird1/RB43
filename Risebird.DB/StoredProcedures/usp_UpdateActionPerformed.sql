/****** Object:  StoredProcedure [dbo].[usp_UpdateVMStatus]    Script Date: 06-10-2020 4.28.48 PM ******/


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateActionPerformed'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateActionPerformed
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateActionPerformed]
@vmId int,
@emailId varchar(100),
@action varchar(100),
@time datetime


AS
Begin

Insert into  VMActionPerformed  values (@vmId, @action, @emailId, @time)

End

GO


