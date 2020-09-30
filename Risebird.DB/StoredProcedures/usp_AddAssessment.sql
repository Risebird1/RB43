IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddAssessment'))
BEGIN
    DROP PROCEDURE dbo.usp_AddAssessment
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_AddAssessment  
@Assessmentgroupid int,  
@Overview varchar(max),  
@Senioritylevel int,  
@YearsofExprience int,  
@AssessmentType int,  
@TestedSkill varchar(200),  
@Evaluation int,  
@Environment varchar(100),  
@Difficultylevel int,  
@EmployerType varchar(50),  
@EmployerStage int,  
@EmployerSize int,  
@createdby int,  
@Delivery varchar(200),
@Assessmentid int
AS  
BEGIN  
if(@Assessmentid=0)
begin
Insert into InterviewerAssessments Output Inserted.id  
values(@Assessmentgroupid,@Overview,@Senioritylevel,@YearsofExprience,@AssessmentType,@TestedSkill,@Evaluation,@Environment,@Difficultylevel,  
@EmployerType,@EmployerStage,@EmployerSize,0,@createdby,Getdate(),@Delivery)  
end
else
begin
update InterviewerAssessments set Assessmentgroupid=@Assessmentgroupid,
Overview=@Overview,
Senioritylevel=@Senioritylevel,
YearsofExprience=@YearsofExprience,
AssessmentType=@AssessmentType,
TestedSkill=@TestedSkill,
Evaluation=@Evaluation,
Environment=@Environment,
Difficultylevel=@Difficultylevel,
EmployerType=@EmployerType,
EmployerStage=@EmployerStage,
EmployerSize=@EmployerSize,
Delivery=@Delivery where id=@Assessmentid

Delete from AssessmentQuestions Where Assessmentid=@Assessmentid
end
END
Go