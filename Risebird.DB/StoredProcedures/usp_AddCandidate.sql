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
@AddedByUserId int

AS
Begin
declare
@ProfileId int = (select id from [Profile] where [Name]='Candidate')
IF NOT EXISTS (SELECT 1   FROM   CandidatesMaster    WHERE  EmailId = @EmailId)
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
		   MobileNumber
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
null
)

declare
@UserId int = (select id from UserList where EmailId=@EmailId and idProfile=@ProfileId)

insert into CandidatesMaster
([UserId],
[Name],
[EmailId],
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
[Notice]
)
values(@UserId,
@Name,
@EmailId,
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
@Notice

)
insert into Candidate
([IRFId],
[Name],
[EmailId],
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
[AddedByUserId]
)
values(@IRFId,
@Name,
@EmailId,
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
@AddedByUserId
)

insert into TransHREvaluatorIRF (IRFId, CandidateId) values (@IRFId, (select id from CandidatesMaster where EmailId=@EmailId))

End

Else

Begin

update CandidatesMaster set 
ResumeLink=@ResumeLink,
[Type]=@Type,
[Location]=@Location,
Skill=@Skill,
WorkAuth=@WorkAuth,
CurrentLocation=@CurrentLocation,
CurrentEmployer=@CurrentEmployer,
Phone=@Phone,
TotalExperience =@TotalExperience,
CurrentSalary=@CurrentSalary,
ExpectedSalary=@ExpectedSalary,
Notice=@Notice
where EmailId=@EmailId


insert into Candidate
([IRFId],
[Name],
[EmailId],
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
[AddedByUserId]
)
values(@IRFId,
@Name,
@EmailId,
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
@AddedByUserId

)

insert into TransHREvaluatorIRF (IRFId, CandidateId) values (@IRFId, (select id from CandidatesMaster where EmailId=@EmailId))

End

END

GO


