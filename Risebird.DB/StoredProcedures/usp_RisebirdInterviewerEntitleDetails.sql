IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_RisebirdInterviewerEntitleDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_RisebirdInterviewerEntitleDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_RisebirdInterviewerEntitleDetails]       
@id int        
AS      
BEGIN   
select distinct inter.id, ul.FirstName,ul.LastName,ul.EmailId,Ul.MobileNumber,      
inter.ResumeLink,inter.IsApproved , inter.UserId ,inter.GitHubLink,inter.LinkedinLink,inter.StackOverflowLink     
from [InterviewerProfile] inter      
inner join [dbo].[UserList] ul on inter.UserId = ul.id  
inner join TransInterviewerSkill tis on tis.InterviewerProfileId=inter.Id  
where ul.IsActive =1 and tis.SkillId=@id  
END  
GO
  
  