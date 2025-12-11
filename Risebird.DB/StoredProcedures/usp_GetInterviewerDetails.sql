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
select intp.*,intp.IsInterest as IsInterested, ul.FirstName, ul.LastName,ul.IsActive, ul.EmailId, ul.MobileNumber, ul.UserPassword, PrimarySkill =       
   STUFF((SELECT distinct  ', ' + Name      
   FROM (select tis.InterviewerProfileId, ski.Name from TransInterviewerSkill tis join Skill ski on tis.SkillId = ski.id ) b      
   WHERE b.InterviewerProfileId = intp.Id      
   FOR XML PATH('')), 1, 1, '') ,irc.Senioritylevel,irc.PerHourRateIndia,irc.PerHourRateOutside,irc.CompentencyLevel,irc.ProfileType, intp.Isavailable,
   CASE
		WHEN intp.Isavailable =1 THEN 'Available'
		WHEN intp.Isavailable =2 THEN 'Not Available'
		WHEN intp.Isavailable =3 THEN 'Need Clarity'
		WHEN intp.Isavailable =4 THEN 'Drop My Profile'
		ELSE 'no data'
	END AS AvailabilityStatusValue
FROM InterviewerProfile intp join UserList ul on ul.id = intp.UserId      
left join InterviewerRecommendation irc on irc.Userid=intp.Userid    
Where ISNULL(intp.IsDeleted,0)=0 
END         
GO


