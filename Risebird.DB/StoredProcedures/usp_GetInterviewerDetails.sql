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
select intp.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber,PrimarySkill = 
			STUFF((SELECT distinct  ', ' + Name
			FROM (select tis.InterviewerProfileId, ski.Name from TransInterviewerSkill tis join Skill ski on tis.SkillId = ski.id ) b
			WHERE b.InterviewerProfileId = intp.Id
			FOR XML PATH('')), 1, 1, '')  
FROM InterviewerProfile intp join UserList ul on ul.id = intp.UserId


END
GO


