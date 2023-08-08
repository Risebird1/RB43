  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveInterviewerEmploymentDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerEmploymentDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_SaveInterviewerEmploymentDetails
@CompanyName varchar(100),
@IsEmployed bit,
@EmploymentComment varchar(50),
@isVerified bit,
@EmailId varchar(150)
AS
BEGIN
Update ip set CompanyName=@CompanyName,IsEmployed=@IsEmployed,EmploymentComment=@EmploymentComment,isVerified=@isVerified
from InterviewerProfile ip join UserList ul on ul.id=ip.UserId where ul.EmailId=@EmailId
END
GO