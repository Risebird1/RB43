/****** Object:  StoredProcedure [dbo].[usp_AddRecruiter]    Script Date: 08-01-2020 6.44.13 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddRecruiter'))
BEGIN
    DROP PROCEDURE dbo.usp_AddRecruiter
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddRecruiter]  
@FirstName varchar(200),  
@LastName varchar(200),  
@EmailId varchar(200),  
@MobileNumber varchar(200),  
@Domain varchar(100)
  
AS  
Begin  
declare  
@ProfileId int = (select id from [Profile] where [Name]='Recruiter')  
IF NOT EXISTS (SELECT 1   FROM   UserList   WHERE  EmailId = @EmailId and idProfile=@ProfileId)  
Begin  
INSERT INTO [dbo].[UserList]  
           (  
     idProfile,  
     FirstName,   
     LastName,  
     EmailId,  
     LastLogin,  
     LastLogout,  
     IsActive,  
     ImageURL,  
     MobileNumber,  
     uniqueid  
           )  
     VALUES  
           (@ProfileId,  
@FirstName,  
@LastName,  
@EmailId,  
null,  
null,  
1,  
null,  
@MobileNumber,  
NEWID()  
)  
  
declare  
@UserId int = (select id from UserList where EmailId=@EmailId and idProfile=@ProfileId)  
  
insert into Recruiter values(@UserId, @Domain)  
  
End  
  
END

GO


