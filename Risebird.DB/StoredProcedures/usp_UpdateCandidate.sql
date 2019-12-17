/****** Object:  StoredProcedure [dbo].[usp_UpdateCandidate]    Script Date: 17-12-2019 6.57.40 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_UpdateCandidate]
@id int,
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
@Notice decimal(10,2)

AS
Begin
declare
@EmailIdOld varchar(200) = (select EmailId from Candidate where id=@id)


update CandidatesMaster set 
[Name]=@Name,
[EmailId] =@EmailId,
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
where EmailId=@EmailIdOld

update Candidate set 
[Name]=@Name,
[EmailId] =@EmailId,
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
where EmailId=@EmailIdOld

End

GO


