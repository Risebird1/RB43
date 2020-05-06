/****** Object:  StoredProcedure [dbo].[usp_GetInterviewerDetails]    Script Date: 5/1/2020 3:55:54 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetInterviewerDetails]
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO





CREATE PROCEDURE [dbo].[usp_GetInterviewerDetails] 
  
AS
BEGIN
select Ipr.id as [id], Ipr.UserId, ul.FirstName,ul.LastName,ul.EmailId,Ul.MobileNumber, 
Ipr.NickName,Ipr.AltEmailID,Ipr.DOB , Ipr.Gender, Ipr.AnnualSalary, Ipr.Country, 
Ipr.City, Ipr.AltMobileNumber, Ipr.CurrentRole, Ipr.Experience, Ipr.PreferredDomain, Ipr.GitHubLink,
Ipr.ResumeLink, Ipr.CodedResumeLink, Ipr.StackOverflowLink, Ipr.LinkedinLink, Ipr.Blog, Ipr.IsLeadInterviewer,
Ipr.IsApproved, ul.IsActive
from [InterviewerProfile] Ipr
inner join [dbo].[UserList] ul on Ipr.UserId = ul.id


END
GO


