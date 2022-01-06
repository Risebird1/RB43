
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddCertificates'))
BEGIN
    DROP PROCEDURE dbo.usp_AddCertificates
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE Procedure usp_AddCertificates  
@Emailid varchar(150),  
@year varchar(15),  
@comment varchar(250),  
@clink varchar(1000),  
@month varchar(15),  
@id int  
As  
BEGIN  
Declare @interviewerid int  
Select @interviewerid=id from Userlist where Emailid=@Emailid  
if Exists (Select 1 from InterviewerCertificates where id=@id )  
BEGIN  
Update InterviewerCertificates set certificatelink=@clink,month=@month,comment=@comment where id=@id  
END  
ELSE  
BEGIN  
Insert into InterviewerCertificates(interviewerid,year,month,certificatelink,comment) values(@interviewerid,@year,@month,@clink,@comment)  
END  
END  
Go