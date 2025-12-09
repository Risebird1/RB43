IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAssessmentGroupInterviewerMembers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAssessmentGroupInterviewerMembers
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetAssessmentGroupInterviewerMembers          
@Assessmentid int          
AS          
BEGIN       
    
if exists(select top 1 id from MasterAssessmentGroup where AssessmentGroupId = @Assessmentid)    
Begin    
    
Declare @temp as Table(  
AssessmentGroupId int  
)  
Insert into @temp select AssessmentGroupId from SecondaryAssessmentGroups where MAGId = (select id from MasterAssessmentGroup where AssessmentGroupId = @Assessmentid)  
Insert into @temp values(@Assessmentid)  
;With CTE AS(          
Select * from InterviewerProfile cross Apply string_split(AssessmentGroupId,','))         
Select Distinct 'Interviewer' as [Type],CTE.UserId,CTE.AssessmentGroupId, ul.EmailId, ul.MobileNumber,ul.uniqueid,    
(ul.FirstName+ space(1) +ul.LastName) as InterviewerName, CTE.ResumeLink,CTE.Isavailable as AvailableStatus,ul.IsActive,   
CTE.isVerified,CTE.IsEmployed,CTE.CompanyName,CTE.EmploymentComment,CTE.AvailabilityResponseDate,    
CTE.RedMarker, agla.CreatedOn as AGAvailabilityCreatedAt, agla.IsAvailable as IsAGLevelAvailable,   
agla.ModifiedAt as AGAvailabilityModifiedAt,  
(SELECT primarySkill = STRING_AGG(sk.[Name], ',') from TransInterviewerSkill tis join Skill sk on sk.id = tis.SkillId where tis.InterviewerProfileId = CTE.id ) as PrimarySkill             
From CTE join Userlist ul on CTE.Userid=ul.id           
join @temp tp on tp.AssessmentGroupId=CTE.value  
left join InterviewMeetings im on im.interviewer=ul.EmailId  and (im.confirmationStatus=1 or im.confirmationStatus is null)          
--left join InterviewerAssessmentVerifications iav on iav.UserId=ul.id and iav.AssessmentId=CTE.value      
left join AssessmentGroup ag on ag.id=CTE.value      
left join MCQSets ms on ms.id=ag.MCQSetsValue      
left join CodeInviteDetails cid on cid.ProgrammingLanguage= ag.CodeLanguage and cid.candidateEmail=ul.EmailId    
--left join MCQShareList msl on msl.MCQId=ms.PartitionKey and msl.EmailAddress=ul.EmailId    
--left join AGLevelAvailability agla on agla.InterviewerProfileId = cte.id    
outer apply ((select top 1 CreatedOn, IsAvailable, ModifiedAt from AGLevelAvailability where AssessmentGroupId = @Assessmentid and InterviewerProfileId = (select id from InterviewerProfile where UserId = CTE.UserId) order by id desc)) agla  
where ul.IsActive=1 and (CTE.IsAvailable != 4 or CTE.Isavailable is null) and ISNULL(CTE.IsDeleted,0)=0  
group by CTE.id, CTE.UserId,CTE.AssessmentGroupId, ul.EmailId, ul.MobileNumber,ul.uniqueid,ul.FirstName+space(1)+ul.LastName,CTE.ResumeLink,CTE.Isavailable,ul.IsActive,   
CTE.isVerified,CTE.IsEmployed,CTE.CompanyName,CTE.EmploymentComment,CTE.AvailabilityResponseDate,CTE.RedMarker ,agla.IsAvailable, agla.CreatedOn, agla.ModifiedAt      
order by ul.EmailId   
End    
else    
begin    
;With CTE AS(          
Select * from InterviewerProfile cross Apply string_split(AssessmentGroupId,','))         
Select Distinct 'Interviewer' as [Type],CTE.UserId,CTE.AssessmentGroupId, ul.EmailId, ul.MobileNumber,ul.uniqueid,    
(ul.FirstName+ space(1) +ul.LastName) as InterviewerName,    
count(im.id) as TotalInterviews, CTE.ResumeLink,CTE.Isavailable as AvailableStatus,ul.IsActive,    
iav.MCQCandidateid,ag.AssessmentVerificationType,ag.CodeLanguage,ms.PartitionKey as MCQSetsValue,cid.inviteId,    
msl.PartitionKey as SMCQId,CTE.isVerified,CTE.IsEmployed,CTE.CompanyName,CTE.EmploymentComment,CTE.AvailabilityResponseDate,    
CTE.RedMarker, agla.CreatedOn as AGAvailabilityCreatedAt, agla.IsAvailable as IsAGLevelAvailable,  
agla.ModifiedAt as AGAvailabilityModifiedAt,  
(SELECT primarySkill = STRING_AGG(sk.[Name], ',') from TransInterviewerSkill tis join Skill sk on sk.id = tis.SkillId where tis.InterviewerProfileId = CTE.id ) as PrimarySkill           
From CTE join Userlist ul on CTE.Userid=ul.id           
left join InterviewMeetings im on im.interviewer=ul.EmailId  and (im.confirmationStatus=1 or im.confirmationStatus is null)          
left join InterviewerAssessmentVerifications iav on iav.UserId=ul.id and iav.AssessmentId=CTE.value      
left join AssessmentGroup ag on ag.id=CTE.value      
left join MCQSets ms on ms.id=ag.MCQSetsValue      
left join CodeInviteDetails cid on cid.ProgrammingLanguage= ag.CodeLanguage and cid.candidateEmail=ul.EmailId    
left join MCQShareList msl on msl.MCQId=ms.PartitionKey and msl.EmailAddress=ul.EmailId    
--left join AGLevelAvailability agla on agla.InterviewerProfileId = cte.id    
outer apply ((select top 1 CreatedOn, IsAvailable, ModifiedAt from AGLevelAvailability where AssessmentGroupId = @Assessmentid and InterviewerProfileId = (select id from InterviewerProfile where UserId = CTE.UserId) order by id desc)) agla  
where CTE.value = @Assessmentid and ul.IsActive=1 and (CTE.IsAvailable != 4 or CTE.Isavailable is null)  and ISNULL(CTE.IsDeleted,0)=0     
group by CTE.id, CTE.UserId,CTE.AssessmentGroupId, ul.EmailId, ul.MobileNumber,ul.uniqueid,ul.FirstName+space(1)+ul.LastName,CTE.ResumeLink,CTE.Isavailable,ul.IsActive,iav.MCQCandidateid,ag.AssessmentVerificationType,    
ag.CodeLanguage,ms.PartitionKey,cid.inviteId,msl.PartitionKey,CTE.isVerified,CTE.IsEmployed,CTE.CompanyName,CTE.EmploymentComment,CTE.AvailabilityResponseDate,CTE.RedMarker, agla.IsAvailable, agla.CreatedOn, agla.ModifiedAt      
order by EmailId  
End     
    
END    
GO

  
      