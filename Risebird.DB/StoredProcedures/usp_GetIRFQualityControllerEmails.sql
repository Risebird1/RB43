/****** Object:  StoredProcedure [dbo].[usp_GetAllUserVMs]    Script Date: 06-10-2020 7.49.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFQualityControllerEmails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFQualityControllerEmails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFQualityControllerEmails]
@IRFId varchar(200) 

AS
Begin

declare @irfIntId int
	set @irfIntId = (select id from IRF where IRFId =@IRFId)
		

	select distinct ul.EmailId from TransQualityController tqc join UserList ul on ul.id = tqc.UserId where IRFId = @irfIntId and tqc.IsActive=1

		


End

GO


