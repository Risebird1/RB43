IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetFLNAcknowledgementStatus'))
BEGIN
    DROP PROCEDURE usp_GetFLNAcknowledgementStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetFLNAcknowledgementStatus    
@startDate datetime = null,                  
@endDate datetime = null,                          
@timeNow datetime,     
@loggedInUser varchar(200)          
AS                  
BEGIN         
    

select distinct poi.EmailId, poi.PrimarySpecialization, poi.AIVerdict, poi.CreatedBy as FLN, poi.CreatedAt as AddedOn,
ipr.CompanyName, ipr.ResumeLink, ul.MobileNumber, ul.FirstName + ' ' + ul.LastName as [Name], irf.IRFId as TargetSkillChannel, 
ag.AssessmentGroup,
CASE 
        WHEN ul.IsActive = 1 THEN 'Acknowledged'
        ELSE ''
    END AS AcknowledgementStatus
from PreOnboardingInterviewerAIVerdict poi
join UserList ul on ul.EmailId = poi.EmailId
join InterviewerProfile ipr on ipr.UserId = ul.id
outer apply (select top 1 IRFId from TransInterviewer where UserId = ul.id order by CreatedAt) a
left join irf on irf.id = a.IRFId
left join AssessmentGroup ag on ag.id = irf.AssessmentGroupId
where (@startDate IS NULL OR poi.CreatedAt >= @startDate)
    AND (@endDate IS NULL OR poi.CreatedAt <= @endDate)
order by poi.CreatedAt desc



END 
GO