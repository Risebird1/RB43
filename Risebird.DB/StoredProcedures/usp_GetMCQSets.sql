IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMCQSets'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMCQSets
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetMCQSets
@profileName varchar(20),
@clientname varchar(150)
AS
BEGIN
if(@profileName='Client')  
begin  
Select ms.PartitionKey, ms.id, ms.Title,ms.CompentencyLevel,ms.PrimarySkills,ms.ProfileType,ms.SecondarySkill, ms.Clients,
ms.Timer, ms.QuestionCount,Count(msl.id) TotalInvite,Count(msc.id) CandidateAttempt, ms.EmploymentType, ms.SeniorityLevel, 
ms.CompanyType, ms.CountryType
From MCQSets ms left join MCQShareList msl on msl.MCQId=ms.PartitionKey and msl.ApplyingCompany=@clientname left join MCQCandidates msc on msc.MCQId=ms.PartitionKey and msc.CandidateEmail=msl.EmailAddress  
group by ms.PartitionKey, ms.id, ms.Title,ms.CompentencyLevel,ms.PrimarySkills,ms.ProfileType,ms.SecondarySkill,
ms.Timer,ms.QuestionCount, ms.Clients, ms.EmploymentType, ms.SeniorityLevel, ms.CompanyType, ms.CountryType 
end  
else  
begin  
Select ms.PartitionKey, ms.id, ms.Title,ms.CompentencyLevel,ms.PrimarySkills,ms.ProfileType,ms.SecondarySkill,ms.Timer, ms.Clients,
ms.QuestionCount,Count(msl.id) TotalInvite,Count(msc.id) CandidateAttempt,  ms.EmploymentType, ms.SeniorityLevel, 
ms.CompanyType, ms.CountryType  
From MCQSets ms left join MCQShareList msl on msl.MCQId=ms.PartitionKey left join MCQCandidates msc on msc.MCQId=ms.PartitionKey and msc.CandidateEmail=msl.EmailAddress  
group by ms.PartitionKey, ms.id, ms.Title,ms.CompentencyLevel,ms.PrimarySkills,ms.ProfileType,ms.SecondarySkill,
ms.Timer,ms.QuestionCount, ms.Clients, ms.EmploymentType, ms.SeniorityLevel, ms.CompanyType, ms.CountryType  
end  
END
Go
