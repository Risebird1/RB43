IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviwerVideoKYCDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviwerVideoKYCDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetInterviwerVideoKYCDetails  
AS  
Begin  
Select ip.Id,ip.UserId,ip.VideoLink,ip.IsVideoApproved,CONCAT(ul.FirstName,'',ul.LastName) as Name ,ul.EmailId,ul.IsActive from InterviewerProfile ip join UserList ul on ul.id=ip.UserId where isnull(ip.IsVideoApproved,0)=0 and ul.IsActive=1 and ip.VideoLink is not null and ISNULL(ip.IsDeleted,0)=0 
End  
GO