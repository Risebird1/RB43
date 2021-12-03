/****** Object:  StoredProcedure [dbo].[usp_AddResumeEvaluator]    Script Date: 23-12-2019 8.22.06 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddResumeEvaluator'))
BEGIN
    DROP PROCEDURE dbo.usp_AddResumeEvaluator
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_AddResumeEvaluator]
@FirstName varchar(200),
@LastName varchar(200),
@EmailId varchar(200),
@MobileNumber varchar(200),
@uniqueid varchar(50)

AS
Begin
declare
@ProfileId int = (select id from [Profile] where [Name]='ResumeEvaluator')
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
@uniqueid
)

declare
@UserId int = (select id from UserList where EmailId=@EmailId and idProfile=@ProfileId)

insert into ResumeEvaluator values(@UserId)

End

END

GO


