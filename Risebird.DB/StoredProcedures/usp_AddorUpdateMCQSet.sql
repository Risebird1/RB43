IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddorUpdateMCQSet'))
BEGIN
    DROP PROCEDURE dbo.usp_AddorUpdateMCQSet
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
CREATE Procedure usp_AddorUpdateMCQSet  
@PartitionKey varchar(50),  
@Title varchar(50),  
@PrimarySkills varchar(100),  
@ProfileType varchar(50),  
@SecondarySkill varchar(100),  
@Timer varchar(5),  
@QuestionCount int,  
@id int, 
@SeniorityLevel varchar(50),
@EmploymentType varchar(20),
@CountryType int = null, 
@CompanyType int = null, 
@Clients varchar(max) = null,
@mcqid int output              
AS  
BEGIN  
if(@id>0)  
begin  
Update MCQSets set Title=@Title,PrimarySkills=@PrimarySkills,  
ProfileType=@ProfileType,SecondarySkill=@SecondarySkill,Timer=@Timer,QuestionCount=@QuestionCount,
SeniorityLevel = @SeniorityLevel, EmploymentType =@EmploymentType,
CompanyType = @CompanyType, CountryType = @CountryType, Clients = @Clients
where id=@id and PartitionKey=@PartitionKey   
select @mcqid = @id   
end  
else   
begin  
Insert into MCQSets (PartitionKey,Title,PrimarySkills,ProfileType,SecondarySkill,Timer,QuestionCount,CreatedOn,
SeniorityLevel, EmploymentType, CompanyType, CountryType, Clients)   
values(@PartitionKey,@Title, @PrimarySkills,@ProfileType,@SecondarySkill,@Timer,@QuestionCount,GetDate(),
@SeniorityLevel, @EmploymentType, @CompanyType, @CountryType, @Clients)  
select @mcqid = SCOPE_IDENTITY()   
end  
END  
Go