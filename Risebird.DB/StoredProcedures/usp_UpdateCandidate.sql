/****** Object:  StoredProcedure [dbo].[usp_UpdateCandidate]    Script Date: 17-12-2019 6.57.40 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_UpdateCandidate]      
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
@Notice decimal(10,2),      
@CostEstimate nvarchar(200),      
@EncodedResumeLink varchar(500),    
@Heuristicsscore int,    
@AvailableFullTime  bit,    
@AvailableFreeLancing bit,    
@ClientHRAttemptContact varchar(100),    
@ProfileSource varchar(200),  
@WhatsappNumber varchar(15),  
@IsProctor bit,  
@PreReportLink varchar(500),
@PreReportUsername varchar(50),
@PreReportPassword varchar(50)
      
AS      
Begin      
declare      
@CandidateUserId int = (select ul.id from Candidate ca join CandidatesMaster cm on cm.id = ca.CandidateMasterId       
join UserList ul on ul.id =cm.UserId where ca.id=@id)    

declare @UserIdOfTheNewEmailId int  = (select id from UserList where EmailId = @EmailId)

if(@CandidateUserId != @UserIdOfTheNewEmailId and @UserIdOfTheNewEmailId is not null and @UserIdOfTheNewEmailId > 0) -- another entry with the same email the parameter holds already exists
Begin

	declare @CandidateMasterId int  = (select id from CandidatesMaster where UserId = @UserIdOfTheNewEmailId)

	update Candidate set CandidateMasterId = @CandidateMasterId where id =@id

	update UserList set FirstName=@Name, MobileNumber =@Phone, WhatsappNumber=@WhatsappNumber where id = @UserIdOfTheNewEmailId
End
else
Begin
	update UserList set EmailId = @EmailId, FirstName=@Name, MobileNumber =@Phone,WhatsappNumber=@WhatsappNumber where id = @CandidateUserId 
end
     
      
update Candidate set       
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
Notice=@Notice,      
CostEstimate=@CostEstimate,      
EncodedResumeLink=@EncodedResumeLink,    
Heuristicsscore=@Heuristicsscore,    
AvailableFullTime=@AvailableFullTime,    
AvailableFreeLancing=@AvailableFreeLancing,    
ClientHRAttemptContact = @ClientHRAttemptContact,    
--ProfileSource=@ProfileSource,  
IsProctor=@IsProctor,  
PreReportLink=@PreReportLink,
PreReportUsername=@PreReportUsername,
PreReportPassword=@PreReportPassword
where id=@id      
      
End     


GO