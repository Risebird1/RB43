/****** Object:  StoredProcedure [dbo].[usp_GetVMQuestions]    Script Date: 02-10-2020 8.18.01 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetVMQuestions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetVMQuestions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetVMQuestions]
@vmId int

AS  
Begin 


select * from ProctorVMQuestions where VmId = @vmId


End
GO


