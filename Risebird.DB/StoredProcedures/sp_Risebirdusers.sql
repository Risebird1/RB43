IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_Risebirdusers'))
BEGIN
    DROP PROCEDURE dbo.sp_Risebirdusers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_Risebirdusers] 
  
AS
BEGIN
select ul.id,ul.idprofile,ul.FirstName,ul.LastName
,ul.EmailId,ul.MobileNumber,ul.isactive, pf.[Name] as ProfileName 
from [dbo].[UserList] ul
join [dbo].[Profile] pf on ul.idprofile = pf.id
where pf.Name in('Admin','HR','Finance','InterviewerManagement') order by pf.Name

END
GO 




