/****** Object:  StoredProcedure [dbo].[usp_FetchUserProfile]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchUserProfile'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchUserProfile
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchUserProfile]
@Emailid varchar(200)
AS
Begin

Select ul.FirstName, Ul.LastName, ul.MobileNumber, ul.WhatsappNumber, up.Name as CurrentRole, ul.ImageURL
from Userlist ul
join Profile up on ul.idProfile = up.id
where Emailid = @Emailid

End

GO
