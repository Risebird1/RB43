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
@Category int 
AS          
BEGIN      
if(@id=0)    
Begin    
Insert into AssessmentGroup(AssessmentGroup,PrimarySkills,Isinstaavailable,Categoryid) values(@AssessmentGroup,@PrimarySkills,@Isinstaavailable,@Category)    
END    
else    
Begin    
Update AssessmentGroup set AssessmentGroup=@AssessmentGroup,PrimarySkills=@PrimarySkills,Isinstaavailable=@Isinstaavailable,Categoryid=@Category where id=@id    
end    
END  

GO