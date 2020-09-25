/****** Object:  StoredProcedure [dbo].[usp_UpdateVMNamePassword]    Script Date: 25-09-2020 5.28.37 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateVMNamePassword'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateVMNamePassword
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateVMNamePassword] 
@id int, 
@CustomName varchar(20),    
@Password varchar(20)    

AS
BEGIN
update [dbo].[VMProperties] 
set
[CustomName]=@CustomName,
[Password]=@Password
where id =@id
 
END
GO


