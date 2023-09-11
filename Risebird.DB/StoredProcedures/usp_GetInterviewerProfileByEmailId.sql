/****** Object:  StoredProcedure [dbo].[usp_GetInterviewerProfileByEmailId]    Script Date: 27-05-2020 8.51.26 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerProfileByEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerProfileByEmailId
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerProfileByEmailId]

@EmailId varchar(50),
@Profile varchar(50)

AS
Begin



select Ipr.id as [id], Ipr.UserId, ul.FirstName,ul.LastName,ul.EmailId,Ul.MobileNumber, 
Ipr.NickName,Ipr.AltEmailID,Ipr.DOB , Ipr.Gender, Ipr.AnnualSalary, Ipr.Country, 
Ipr.City, Ipr.AltMobileNumber, Ipr.CurrentRole, Ipr.Experience, Ipr.PreferredDomain, Ipr.GitHubLink,
Ipr.ResumeLink, Ipr.CodedResumeLink, Ipr.StackOverflowLink, Ipr.LinkedinLink, Ipr.Blog, Ipr.IsLeadInterviewer,
Ipr.IsApproved, ul.IsActive,Ipr.Isinstaavailable,Ipr.Isavailable,Ipr.VideoConfirmation  
from [InterviewerProfile] Ipr
inner join [dbo].[UserList] ul on Ipr.UserId = ul.id  
WHERE ul.Emailid = @EmailId 


End
GO


