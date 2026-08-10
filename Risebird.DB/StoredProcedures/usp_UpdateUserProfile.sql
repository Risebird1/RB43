/****** Object:  StoredProcedure [dbo].[usp_UpdateUserProfile]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateUserProfile'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateUserProfile
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateUserProfile]
@FirstName varchar(200),
@LastName varchar(200),
@Emailid varchar(200),
@MobileNumber varchar(50),
@WhatsappNumber varchar(50),
@ImageURL varchar(max) = NULL
AS
Begin

Update Userlist
Set FirstName = @FirstName,
    LastName = @LastName,
    MobileNumber = @MobileNumber,
    WhatsappNumber = @WhatsappNumber,
    ImageURL = IsNull(@ImageURL, ImageURL)
where Emailid = @Emailid

End

GO
