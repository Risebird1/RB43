/****** Object:  StoredProcedure [dbo].[usp_UpdateMeeting]    Script Date: 07-07-2020 11.46.28 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateMeeting'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateMeeting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_UpdateMeeting]    
@interviewId int,
@IRFId varchar(200),
@interviewDate varchar(200),
@interviewSlot varchar(100),
@interviewDuration int,
@candidateEmail varchar(200),
@hrRiseBirdAssinged varchar(200),
@resumeLink varchar(MAX),
@zoomLink varchar(MAX),
@ClientHrContact varchar(200),
@comfirmationStatus varchar(100),
@interviewer varchar(200),
@preferredLocation varchar(200),
@expectedPerHourRate varchar(200),
@joiningTime varchar(200),
@staffingAgency varchar(200),
@candidateName varchar(200),
@currentEmployerName varchar(200),
@MobileNumber nvarchar(200),
@HRCoordinatorEmailId varchar(100),
--@QCInterviewer varchar(200),
@zoomMeetingId varchar(200),
@IsEmergency bit,
@HasDistinctParticipants bit

AS
Begin
   declare 
	   @irfIntId int, @oldConfirmationStatus varchar(100)
   
   set @irfIntId =(select id from IRF where IRFId=@IRFId)
   set @candidateEmail = trim(@candidateEmail)
	

	IF EXISTS (SELECT 1   FROM   InterviewMeetings  WHERE  id=@interviewId)
		Begin
		set @oldConfirmationStatus = (select  @oldConfirmationStatus from InterviewMeetings where id =@interviewId)
			UPDATE  [dbo].[InterviewMeetings]
			SET         [IRFId] = @IRFId
					   ,[interviewDate] = @interviewDate
					   ,[interviewSlot] = @interviewSlot
					   ,[interviewDuration] = @interviewDuration
					   ,[candidateEmail] = @candidateEmail
					   ,[candidateName] = @candidateName
					   ,[ClientHrContact] = @ClientHrContact
					   ,[resumeLink] = @resumeLink
					   ,[confirmationStatus] = @comfirmationStatus
					   ,[zoomLink] = @zoomLink
					   ,[risebirdHR] = @hrRiseBirdAssinged
					   ,[PreferredLocation] = @preferredLocation
					   ,[ExpectedPerHourRate] = @expectedPerHourRate
					   ,[joiningTime] = @joiningTime
					   ,[staffingAgency] = @staffingAgency	   
					   ,[sModifiedBy] ='System'
					   ,[dtModifiedOn] = GETUTCDATE()
					   ,[currentEmployerName] = @currentEmployerName
					   ,[MobileNumber]= @MobileNumber
					   ,[interviewer]= @interviewer
					   ,[HRCoordinatorEmailId] =@HRCoordinatorEmailId
					   ,[ZoomMeetingId]=@zoomMeetingId
					   ,[IsEmergency]=@IsEmergency
					   ,[HasDistinctParticipants]=@HasDistinctParticipants
			WHERE id = @interviewId        
    
		End

	if(@comfirmationStatus='2')
		IF EXISTS (SELECT 1   FROM   InterviewComments  WHERE  InterviewId=@interviewId)
		Begin
			update InterviewComments set MeetingStatus=null where InterviewId = @interviewId
		End

	declare 
	@canMasterId int,
	@profileId int 

	set @profileId = (select id from [Profile] where [Name]='Candidate')

IF NOT EXISTS (SELECT 1  FROM  UserList  WHERE  EmailId = @candidateEmail)
Begin
	--declare
	--@profileId int = (select id from [Profile] where [Name]='Candidate')
	
	Insert into UserList (idProfile, FirstName, EmailId, IsActive,MobileNumber) values (@profileId, @candidateName, @candidateEmail, 1,@MobileNumber)

	declare 
	@cndUserId int = (select id from UserList where EmailId=@candidateEmail and idProfile=@profileId)

	Insert into CandidatesMaster (UserId) values(@cndUserId)

	set @canMasterId = (select id from CandidatesMaster where UserId=@cndUserId)

	Insert into Candidate (IRFId, ResumeLink, [Location],Phone, CandidateMasterId, ClientHRAttemptContact, ProfileSource) 
	values((select id from IRF where IRFId=@IRFId), @resumeLink, @preferredLocation, @MobileNumber,
	(select id from CandidatesMaster  where UserId = @cndUserId), @ClientHrContact, @staffingAgency)

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

	set @canMasterId = (select id from CandidatesMaster where UserId=@candidateUserId)
	--declare @irfintid int = (select id from IRF where IRFId=@IRFId)
	IF NOT EXISTS (SELECT 1  FROM  Candidate  WHERE  CandidateMasterId = @canMasterId and IRFID= @irfintid)
		Begin
			Insert into Candidate (IRFId, ResumeLink, [Location],Phone, CandidateMasterId, ClientHRAttemptContact, ProfileSource) 
			values(@irfintid, @resumeLink, @preferredLocation,@MobileNumber, @canMasterId, @ClientHrContact, @staffingAgency) 	
			
			--insert candidate added status
			insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy) 
			values (@irfintid, @canMasterId, 'Added', GETDATE(), 12, 'System')
		End

End


if(@oldConfirmationStatus != @comfirmationStatus)
Begin
	if(@comfirmationStatus='2')	
	begin
		insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy, InterviewId) 
		values (@irfIntId, @canMasterId, 'Cancelled', GETDATE(), 18, 'System', @interviewId)
	end
	else
	begin
		insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy, InterviewId) 
		values (@irfIntId, @canMasterId, 'Scheduled', GETDATE(), 4, 'System', @InterviewId)
	end
End


--declare @canUserId int,@canMstid int, @idIrf int;

--set @canUserId = (select id from UserList where EmailId = @candidateEmail and idProfile=@profileId)
--set @canMstid =(select id from CandidatesMaster where UserId=@canUserId)
--set @idIrf =(select id from IRF where IRFId=@IRFId)

--if(@comfirmationStatus='2')
--	Begin
--		insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy) 
--		values (@idIrf, @canMstid, 'Cancelled', GETDATE(), 18, 'System')
--	End

--Else
--	Begin
--		insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy) 
--		values (@idIrf, @canMstid, 'Cancelled', GETDATE(), 18, 'System')
--	End




END






GO


