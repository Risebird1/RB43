/****** Object:  StoredProcedure [dbo].[usp_UpdateRecruiter]    Script Date: 08-01-2020 6.44.32 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateRecruiter'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateRecruiter
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateRecruiter]
@UserId int,
@FirstName varchar(200),
@LastName varchar(200),
@EmailId varchar(200),
@Domain varchar(100),
@MobileNumber varchar(200)

AS
Begin

update UserList set 
[FirstName]=@FirstName,
[LastName]=@LastName,
[EmailId] =@EmailId,
[MobileNumber]=@MobileNumber
where id=@UserId

update Recruiter set 
[Domain] =@Domain
where UserId=@UserId

End

GO


