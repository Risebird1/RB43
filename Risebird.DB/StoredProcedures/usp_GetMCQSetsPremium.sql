IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMCQSetsPremium'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMCQSetsPremium
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetMCQSetsPremium  
@profileName varchar(20),  
@clientname varchar(150),
@requesttype bit
AS  
BEGIN  
Select ms.PartitionKey, ms.id, ms.Title,ms.CompentencyLevel,ms.PrimarySkills,ms.ProfileType,ms.SecondarySkill, ms.Clients,  
ms.Timer, ms.QuestionCount,Count(msl.id) TotalInvite,Count(msc.id) CandidateAttempt, ms.EmploymentType, ms.SeniorityLevel,   
ms.CompanyType, ms.CountryType,ms.AadhaarVerification,ms.PremiumMCQ  
From MCQSets ms left join MCQShareListPremium msl on msl.MCQId=ms.PartitionKey and msl.ApplyingCompany=@clientname left join MCQCandidatesPremium msc on msc.MCQId=ms.PartitionKey and msc.CandidateEmail=msl.EmailAddress
group by ms.PartitionKey, ms.id, ms.Title,ms.CompentencyLevel,ms.PrimarySkills,ms.ProfileType,ms.SecondarySkill,  
ms.Timer,ms.QuestionCount, ms.Clients, ms.EmploymentType, ms.SeniorityLevel, ms.CompanyType, ms.CountryType,ms.AadhaarVerification,ms.PremiumMCQ   

END 

Go
