/****** Object:  StoredProcedure [dbo].[usp_AddCandidate]    Script Date: 29-11-2019 4.27.37 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_AddCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_AddCandidate]    
@IRFId int,    
@Name varchar(200),    
@EmailId varchar(200),    
@ResumeLink varchar(500),    
@Type varchar(200),    
@Location varchar(200),    
@Skill varchar(200),    
@WorkAuth varchar(200),     
@CurrentLocation varchar(200),    
@CurrentEmployer varchar(200),    
@Phone varchar(200),    
@TotalExperience decimal(10,2),    
@CurrentSalary decimal(10,2),    
@ExpectedSalary decimal(10,2),    
@Notice decimal(10,2),    
@AddedByUserId int,    
@CostEstimate nvarchar(200),    
@EncodedResumeLink varchar(500),  
@Heuristicsscore int,  
@AvailableFullTime  bit,  
@AvailableFreeLancing bit,  
@ClientHRAttemptContact varchar(100),  
@ProfileSource varchar(200),
@WhatsappNumber varchar(15),
@uniqueid varchar(50)    
AS    
Begin    
declare    
@ProfileId int = (select id from [Profile] where [Name]='Candidate')    
IF NOT EXISTS (SELECT 1   FROM   UserList    WHERE  EmailId = @EmailId)    
Begin    
INSERT INTO [dbo].[UserList]    
           (    
     idProfile,    
     FirstName,     
     LastName,    
     EmailId,    
     LastLogin,    
     LastLogout,    
     IsActive,    
     ImageURL,    
     MobileNumber,
	 WhatsappNumber,
	 uniqueid
           )    
     VALUES    
           (23,    
@Name,    
null,    
@EmailId,    
null,    
null,    
1,    
null,    
@Phone,
@WhatsappNumber,
@uniqueid
)    
    
declare    
@UserId int = (select id from UserList where EmailId=@EmailId and idProfile=@ProfileId)    
    
insert into CandidatesMaster ([UserId]) values (@UserId)    
    
insert into Candidate    
([IRFId],    
[ResumeLink],    
[Type],    
[Location],    
[Skill],    
[WorkAuth],    
[CurrentLocation],    
[CurrentEmployer],    
[Phone],    
[TotalExperience],    
[CurrentSalary],    
[ExpectedSalary],    
[Notice],    
[AddedByUserId],    
[CandidateMasterId],    
[CostEstimate],    
[EncodedResumeLink],  
[Heuristicsscore],  
[AvailableFullTime],  
[AvailableFreeLancing],  
[ClientHRAttemptContact],  
[ProfileSource]  
)    
values(@IRFId,    
@ResumeLink,    
@Type,    
@Location,    
@Skill,    
@WorkAuth,    
@CurrentLocation,    
@CurrentEmployer,    
@Phone,    
@TotalExperience,    
@CurrentSalary,    
@ExpectedSalary,    
@Notice,    
@AddedByUserId,    
(select id from CandidatesMaster where UserId=@UserId),    
@CostEstimate,    
@EncodedResumeLink,  
@Heuristicsscore,  
@AvailableFullTime,  
@AvailableFreeLancing,  
@ClientHRAttemptContact,  
@ProfileSource  
)    
    
insert into TransHREvaluatorIRF (IRFId, CandidateId) values (@IRFId, (select id from CandidatesMaster where UserId=@UserId))    
    
End    
    
Else    
    
Begin    
declare    
    
@CandidateUserId int = (select id from UserList where EmailId=@EmailId and idProfile=@ProfileId)    
    
    
IF NOT EXISTS (SELECT 1   FROM   CandidatesMaster    WHERE  UserId = @CandidateUserId)    
Begin    
 insert into CandidatesMaster ([UserId]) values (@CandidateUserId)    
End    
    
    
insert into Candidate    
([IRFId],    
[ResumeLink],    
[Type],    
[Location],    
[Skill],    
[WorkAuth],    
[CurrentLocation],    
[CurrentEmployer],    
[Phone],    
[TotalExperience],    
[CurrentSalary],    
[ExpectedSalary],    
[Notice],    
[AddedByUserId],    
[CandidateMasterId],    
[CostEstimate],    
[EncodedResumeLink],  
[Heuristicsscore],  
[AvailableFullTime],  
[AvailableFreeLancing],  
[ClientHRAttemptContact],  
[ProfileSource]  
)    
values(@IRFId,    
@ResumeLink,    
@Type,    
@Location,    
@Skill,    
@WorkAuth,    
@CurrentLocation,    
@CurrentEmployer,    
@Phone,    
@TotalExperience,    
@CurrentSalary,    
@ExpectedSalary,    
@Notice,    
@AddedByUserId,    
(select id from CandidatesMaster where UserId=@CandidateUserId),    
@CostEstimate,    
@EncodedResumeLink,  
@Heuristicsscore,  
@AvailableFullTime,  
@AvailableFreeLancing,  
@ClientHRAttemptContact,  
@ProfileSource)    
    
    
insert into TransHREvaluatorIRF (IRFId, CandidateId) values (@IRFId, (select id from CandidatesMaster where  UserId=@CandidateUserId))    
    
End    
    
END  
  
  
  
  
  
  
  
  

GO


