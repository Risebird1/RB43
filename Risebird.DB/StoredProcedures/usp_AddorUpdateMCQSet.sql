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
@CompentencyLevel int,  
@PrimarySkills varchar(100),  
@ProfileType int,  
@SecondarySkill varchar(100),  
@Timer varchar(5),  
@QuestionCount int,  
@id int,  
@mcqid int output              
AS  
BEGIN  
if(@id>0)  
begin  
Update MCQSets set Title=@Title,CompentencyLevel=@CompentencyLevel,PrimarySkills=@PrimarySkills,  
ProfileType=@ProfileType,SecondarySkill=@SecondarySkill,Timer=@Timer,QuestionCount=@QuestionCount where id=@id and PartitionKey=@PartitionKey   
select @mcqid = @id   
end  
else   
begin  
Insert into MCQSets (PartitionKey,Title,CompentencyLevel,PrimarySkills,ProfileType,SecondarySkill,Timer,QuestionCount,CreatedOn)   
values(@PartitionKey,@Title,@CompentencyLevel,@PrimarySkills,@ProfileType,@SecondarySkill,@Timer,@QuestionCount,GetDate())  
select @mcqid = SCOPE_IDENTITY()   
end  
END  
Go