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
@ProfileType int,
@CompetencyLevel int,
@Country int,
@PerHourRate int
AS            
BEGIN        
if(@id=0)      
Begin      
Insert into AssessmentGroup(AssessmentGroup,PrimarySkills,Isinstaavailable,Categoryid,CompanyType,IsRelevant,ProfileType,CompetencyLevel,Country,PerHourRate) 
values(@AssessmentGroup,@PrimarySkills,@Isinstaavailable,@Category,@CompanyType,@IsRelevant,@ProfileType,@CompetencyLevel,@Country,@PerHourRate)      
END      
else      
Begin      
Update AssessmentGroup set AssessmentGroup=@AssessmentGroup,PrimarySkills=@PrimarySkills,Isinstaavailable=@Isinstaavailable,Categoryid=@Category, 
CompanyType=@CompanyType,IsRelevant=@IsRelevant,ProfileType=@ProfileType,CompetencyLevel=@CompetencyLevel,Country=@Country,PerHourRate=@PerHourRate where id=@id      
end      
END    
  

GO