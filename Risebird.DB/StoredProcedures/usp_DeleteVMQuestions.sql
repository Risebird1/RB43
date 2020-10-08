/****** Object:  StoredProcedure [dbo].[usp_DeleteTransVmUsers]    Script Date: 02-10-2020 6.56.40 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteVMQuestions'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteVMQuestions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_DeleteVMQuestions]

@vmId int
AS
Begin

delete from ProctorVMQuestions where VmId=@vmId

End
GO


