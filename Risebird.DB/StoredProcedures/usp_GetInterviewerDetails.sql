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
select c.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber from (SELECT  Id as id, NickName, AltEmailID, DOB, Gender, AnnualSalary, Country, City, AltMobileNumber, CurrentRole, 
Experience, PreferredDomain, GitHubLink, ResumeLink, CodedResumeLink, StackOverflowLink, LinkedinLink, Blog,
IsLeadInterviewer, IsApproved,
UserId, PrimarySkill = 
			STUFF((SELECT ', ' + SkillName
			FROM (select ipr.*, tis.SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id ) b 
			WHERE b.Id = a.Id
			FOR XML PATH('')), 1, 2, '')
FROM (select ipr.*, tis.SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id) a
GROUP BY Id, UserId, NickName, AltEmailID, DOB, Gender, AnnualSalary, Country, City, AltMobileNumber, CurrentRole,
Experience, PreferredDomain, GitHubLink, ResumeLink, CodedResumeLink, StackOverflowLink, LinkedinLink, Blog,
IsLeadInterviewer, IsApproved) c join UserList ul on ul.id = c.UserId


END
GO


