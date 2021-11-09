IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchPasswordresetDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchPasswordresetDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchPasswordresetDetails
@noofdays int
AS
BEGIN
declare @tempRecords table (EmailId varchar(150))  
insert into @tempRecords Select EmailId from UserList Where id in(Select UserId From ClientUser where IsPasswordReset=1) and DATEDIFF(day, PasswordCreation, GetDate())>=@noofdays
Update ul Set ul.PasswordCreation=GETDATE() from UserList ul join @tempRecords CTE on CTE.EmailId=ul.EmailId
Select * from @tempRecords
END
GO