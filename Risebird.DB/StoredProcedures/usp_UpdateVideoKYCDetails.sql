IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateVideoKYCDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateVideoKYCDetails
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateVideoKYCDetails  
@CurrentDate datetime,  
@videolink varchar(1000),
@Emailid varchar(200)
AS  
BEGIN  
Update ip set VideoLink=@videolink,IsVideoApproved=0,LinkAddedOn=@CurrentDate from InterviewerProfile ip join UserList ul on ul.id=ip.Userid where ul.Emailid=@Emailid
END  
GO
