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
ipr.CompanyName, ipr.ResumeLink, ul.MobileNumber, ul.FirstName + ' ' + ul.LastName as [Name],
CASE 
        WHEN ul.IsActive = 1 THEN 'Acknowledged'
        ELSE ''
    END AS AcknowledgementStatus
from PreOnboardingInterviewerAIVerdict poi
join UserList ul on ul.EmailId = poi.EmailId
join InterviewerProfile ipr on ipr.UserId = ul.id
--where IsActive = 1
order by poi.CreatedAt desc



END 
GO