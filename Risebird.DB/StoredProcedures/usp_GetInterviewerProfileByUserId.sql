/****** Object:  StoredProcedure [dbo].[usp_GetInterviewerProfileByUserId]    Script Date: 5/1/2020 4:02:56 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerProfileByUserId'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetInterviewerProfileByUserId]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


  
  
CREATE PROCEDURE [dbo].[usp_GetInterviewerProfileByUserId]    
@UserId int    
AS    
Begin    
   
select Ipr.id as [id], Ipr.UserId, ul.FirstName,ul.LastName,ul.EmailId,Ul.MobileNumber,     
Ipr.NickName,Ipr.AltEmailID,Ipr.DOB , Ipr.Gender, Ipr.AnnualSalary, Ipr.Country,     
Ipr.City, Ipr.AltMobileNumber, Ipr.CurrentRole, Ipr.Experience, Ipr.PreferredDomain, Ipr.GitHubLink,    
Ipr.ResumeLink, Ipr.CodedResumeLink, Ipr.StackOverflowLink, Ipr.LinkedinLink, Ipr.Blog, Ipr.IsLeadInterviewer,    
Ipr.IsApproved, ul.IsActive, Ipr.AssessmentGroupId,ul.WhatsappNumber,Ipr.Isinstaavailable , --Ipr.IsServiceDomain, Ipr.IsProductDomain,
Ipr.IndiaServiceRate, Ipr.IndiaProductRate, Ipr.OutsideIndiaServiceRate, Ipr.OutsideIndiaProductRate
from [InterviewerProfile] Ipr    
inner join [dbo].[UserList] ul on Ipr.UserId = ul.id    
WHERE Ipr.userId = @UserId    
    
    
End    
GO

