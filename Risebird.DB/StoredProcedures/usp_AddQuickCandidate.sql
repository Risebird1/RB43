/****** Object:  StoredProcedure [dbo].[usp_AddCandidate]    Script Date: 29-11-2019 4.27.37 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddQuickCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_AddQuickCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].usp_AddQuickCandidate  
@IRFId int,  
@Name varchar(200),  
@EmailId varchar(200),  
@ResumeLink varchar(500),    
@Phone varchar(200),
@AddedByUserId int  

  
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
@Phone  
)  
  
declare  
@UserId int = (select id from UserList where EmailId=@EmailId and idProfile=@ProfileId)  
  
insert into CandidatesMaster ([UserId]) values (@UserId)  
  
insert into Candidate  
([IRFId],  
[ResumeLink],    
[Phone],  
[AddedByUserId],  
[CandidateMasterId]
)  
values(@IRFId,  
@ResumeLink,   
@Phone,  
@AddedByUserId,  
(select id from CandidatesMaster where UserId=@UserId)

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
[Phone],   
[AddedByUserId],  
[CandidateMasterId]
)  
values(@IRFId,  
@ResumeLink,  
@Phone,  
@AddedByUserId,  
(select id from CandidatesMaster where UserId=@CandidateUserId)
)    
  
insert into TransHREvaluatorIRF (IRFId, CandidateId) values (@IRFId, (select id from CandidatesMaster where  UserId=@CandidateUserId))  
  
End  
  
END


GO


