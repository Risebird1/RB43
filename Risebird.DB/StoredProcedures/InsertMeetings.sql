IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.InsertMeetings'))
BEGIN
    DROP PROCEDURE dbo.InsertMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[InsertMeetings]
@IRFId varchar(200),
@interviewDate varchar(200),
@slot varchar(100),
@interviewDuration int,
@candidateName varchar(200),
@candidateEmail varchar(200),
@hrRiseBirdAssinged varchar(200),
@resumeLink varchar(MAX),
@zoomLink varchar(MAX),
@ClientHrContact varchar(200),
@comfirmationStatus varchar(100),
@prefferedLocation varchar(200),
@expectedPerHourRate varchar(200),
@joiningTime varchar(200),
@staffingAgenecy varchar(200),
@interviewOnDevice varchar(500),
@currentEmployerName varchar(200),
@textreminder nvarchar(200)
AS
Begin
declare @InterviewId int; 

IF NOT EXISTS (SELECT 1   FROM   InterviewMeetings    WHERE  candidateEmail = @candidateEmail and interviewDate=@interviewDate and IRFId= @IRFId)
Begin

INSERT INTO [dbo].[InterviewMeetings]
           ([IRFId]
           ,[interviewDate]
           ,[interviewSlot]
           ,[interviewDuration]
		   ,[candidateName]
           ,[candidateEmail]
           ,[ClientHrContact]
           ,[resumeLink]
           ,[confirmationStatus]
           ,[zoomLink]
           ,[risebirdHR]
		   ,[currentEmployerName]
		   ,[PreferredLocation]
		   ,[ExpectedPerHourRate]
		   ,[joiningTime]
		   ,[staffingAgency]
		   ,[IntervIewOnDevice]
           ,[sCreatedBy]
           ,[dtCreatedOn]
		   ,[MobileNumber]
           )
     VALUES
           (@IRFId,
@interviewDate,
@slot,
@interviewDuration,
@candidateName,
@candidateEmail,
@ClientHrContact,
@resumeLink,
@comfirmationStatus,
@zoomLink,
@hrRiseBirdAssinged,
@currentEmployerName,
@prefferedLocation,
@expectedPerHourRate,
@joiningTime,
@staffingAgenecy,
@interviewOnDevice,
'Clendly',
GETUTCDATE(),
@textreminder  
)

set @InterviewId=SCOPE_IDENTITY()


End


ELSE

Begin

set @InterviewId = (select id from InterviewMeetings where candidateEmail = @candidateEmail and interviewDate=@interviewDate)

 UPDATE [dbo].[InterviewMeetings]
   SET [interviewDate] = @interviewDate
	  ,[ClientHrContact]=@ClientHrContact
      ,[confirmationStatus] = @comfirmationStatus
	  ,[resumeLink] = @resumeLink
	  ,[zoomLink]=@zoomLink
           ,[risebirdHR]=@hrRiseBirdAssinged
		   ,[currentEmployerName]=@currentEmployerName
		   ,[PreferredLocation]=@prefferedLocation
		   ,[ExpectedPerHourRate]=@expectedPerHourRate
		   ,[joiningTime]=@joiningTime
		   ,[staffingAgency]=@staffingAgenecy
 WHERE     candidateEmail = @candidateEmail and interviewDate=@interviewDate
 
 
 
End


if(@comfirmationStatus='1')
	Begin

	 IF NOT EXISTS (SELECT 1   FROM   InterviewComments    WHERE  InterviewId =@InterviewId)
		Begin
			insert into InterviewComments(InterviewId, MeetingStatus) values ( @InterviewId, 2)

		End
	 Else
		Begin
			update InterviewComments set MeetingStatus=2 where InterviewId=@InterviewId
		End


	End

Else
	Begin
	 		update InterviewComments set MeetingStatus = null where InterviewId=@InterviewId

	End

declare
	@profileId int = (select id from [Profile] where [Name]='Candidate')
IF NOT EXISTS (SELECT 1  FROM  UserList  WHERE  EmailId = @candidateEmail)
Begin
	--declare
	--@profileId int = (select id from [Profile] where [Name]='Candidate')
	
	Insert into UserList (idProfile, FirstName, EmailId, IsActive,MobileNumber) values (@profileId, @candidateName, @candidateEmail, 1,@textreminder)

	declare 
	@cndUserId int = (select id from UserList where EmailId=@candidateEmail and idProfile=@profileId)

	Insert into CandidatesMaster (UserId) values(@cndUserId)
	declare
	@canMasterId int = (select id from CandidatesMaster where UserId=@cndUserId)

	Insert into Candidate (IRFId, ResumeLink, [Location],Phone, CandidateMasterId) 
	values((select id from IRF where IRFId=@IRFId), @resumeLink, @prefferedLocation, @textreminder,
	(select id from CandidatesMaster  where UserId = @cndUserId))

	--insert candidate added status
	insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy) 
	values ((select id from IRF where IRFId=@IRFId), @canMasterId, 'Added', GETDATE(), 12, 'System')
	
End

Else

Begin
	declare
	@candidateUserId int = (select id from UserList where EmailId = @candidateEmail)

	IF NOT EXISTS (SELECT 1  FROM  CandidatesMaster  WHERE  UserId = @candidateUserId)
		begin
			Insert into CandidatesMaster (UserId) values(@candidateUserId)
		end

	declare
	@candidateMasterId int = (select id from CandidatesMaster where UserId=@candidateUserId)
	declare @irfintid int = (select id from IRF where IRFId=@IRFId)
	IF NOT EXISTS (SELECT 1  FROM  Candidate  WHERE  CandidateMasterId = @candidateMasterId and IRFID= @irfintid)
		Begin
			Insert into Candidate (IRFId, ResumeLink, [Location],Phone, CandidateMasterId) 
			values(@irfintid, @resumeLink, @prefferedLocation,@textreminder, @candidateMasterId) 	
			
			--insert candidate added status
			insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy) 
			values (@irfintid, @candidateMasterId, 'Added', GETDATE(), 12, 'System')
		End

End

--insert into CandidateStatus table
declare @canUserId int,@canMstid int, @idIrf int;

set @canUserId = (select id from UserList where EmailId = @candidateEmail and idProfile=@profileId)
set @canMstid =(select id from CandidatesMaster where UserId=@canUserId)
set @idIrf =(select id from IRF where IRFId=@IRFId)

if(@comfirmationStatus='1')
	Begin
		insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy) 
		values (@idIrf, @canMstid, 'Scheduled', GETDATE(), 4, 'System')
	End

Else
	Begin
		insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy) 
		values (@idIrf, @canMstid, 'Cancelled', GETDATE(), 18, 'System')
	End
END




GO



