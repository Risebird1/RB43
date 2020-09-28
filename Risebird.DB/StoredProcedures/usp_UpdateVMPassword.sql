/****** Object:  StoredProcedure [dbo].[usp_UpdateVMPassword]    Script Date: 25-09-2020 5.28.28 PM ******/


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateVMPassword'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateVMPassword
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateVMPassword] 
@id int, 
@Password varchar(20)    

AS
BEGIN
update [dbo].[VMProperties] 
set
[Password]=@Password
where id =@id
 
END
GO


