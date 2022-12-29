IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddAssessmentGroup'))
BEGIN
    DROP PROCEDURE dbo.usp_AddAssessmentGroup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
 
   
CREATE Procedure usp_AddAssessmentGroup            
@AssessmentGroup varchar(100),            
@PrimarySkills varchar(max),      
@id int,    
@Isinstaavailable bit,  
@Category int,
@CompanyType int,
@IsRelevant bit,
@ProfileType varchar(50) = null,
@CompetencyLevel int = null,
@Country int,
@PerHourRate int,
@SeniorityLevel varchar(50) = null,
@EmploymentType varchar(20) = null,
@AssessmentVerificationType varchar(5),
@CodeLanguage varchar(100),
@MCQSetsValue int
AS            
BEGIN        
if(@id=0)      
Begin      
Insert into AssessmentGroup(AssessmentGroup,PrimarySkills,Isinstaavailable,Categoryid,CompanyType,IsRelevant,ProfileType,CompetencyLevel,Country, PerHourRate, SeniorityLevel, EmploymentType,AssessmentVerificationType,CodeLanguage,MCQSetsValue) 
values(@AssessmentGroup,@PrimarySkills,@Isinstaavailable,@Category,@CompanyType,@IsRelevant,@ProfileType,@CompetencyLevel,@Country,@PerHourRate, @SeniorityLevel, @EmploymentType,@AssessmentVerificationType,@CodeLanguage,@MCQSetsValue)      
END      
else      
Begin      
Update AssessmentGroup set AssessmentGroup=@AssessmentGroup,PrimarySkills=@PrimarySkills,Isinstaavailable=@Isinstaavailable,Categoryid=@Category, 
CompanyType=@CompanyType,IsRelevant=@IsRelevant,ProfileType=@ProfileType,CompetencyLevel=@CompetencyLevel,Country=@Country,PerHourRate=@PerHourRate,
SeniorityLevel = @SeniorityLevel, EmploymentType = @EmploymentType,AssessmentVerificationType=@AssessmentVerificationType,CodeLanguage=@CodeLanguage,
MCQSetsValue=@MCQSetsValue
where id=@id      
end      
END    
GO