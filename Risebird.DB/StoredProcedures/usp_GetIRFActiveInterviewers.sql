/****** Object:  StoredProcedure [dbo].[usp_GetAllUserVMs]    Script Date: 06-10-2020 7.49.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFActiveInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFActiveInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFActiveInterviewers]
@IRFId varchar(200) 

AS
Begin

declare @irfIntId int
	set @irfIntId = (select id from IRF where IRFId =@IRFId)
		
	Begin
		select distinct ul.EmailId from TransInterviewer ti join UserList ul on ul.id = ti.UserId where IRFId = @irfIntId and ti.IsActive=1
	End

End

GO


