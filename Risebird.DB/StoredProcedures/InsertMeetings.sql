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
@textreminder nvarchar(200),    
@zoomMeetingId bigint,    
@interviewIdOutput int out    
AS    
Begin    
declare @InterviewId int;     
declare @WhatsApp varchar(15) 

Set @WhatsApp= Replace(@textreminder,' ','')  
Set @WhatsApp= Replace(@WhatsApp,'+91','')  

if(LEN(@WhatsApp)!=10)  
begin  
Set @WhatsApp=''  
END  
 
IF EXISTS (Select 1 from IRF where IRFId =@IRFId)    
Begin    
    
    
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
     ,[ZoomMeetingId]    
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
@textreminder,    
@zoomMeetingId    
)    
    
set @InterviewId=SCOPE_IDENTITY()    
set @interviewIdOutput = @InterviewId    
    
    
End    
    
    
ELSE    
    
Begin    
    
set @InterviewId = (select id from InterviewMeetings where candidateEmail = @candidateEmail and interviewDate=@interviewDate and IRFId= @IRFId)    
set @interviewIdOutput = @InterviewId    
    
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
     ,[staffingAgency]=@staffingAgenecy,    
     [ZoomMeetingId]=@zoomMeetingId    
 WHERE     id=@InterviewId    
     
     
     
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
     
 Insert into UserList (idProfile, FirstName, EmailId, IsActive,MobileNumber,WhatsappNumber) values (@profileId, @candidateName, @candidateEmail, 1,@textreminder,@WhatsApp)    
    
 declare     
 @cndUserId int = (select id from UserList where EmailId=@candidateEmail and idProfile=@profileId)    
    
 Insert into CandidatesMaster (UserId) values(@cndUserId)    
 declare    
 @canMasterId int = (select id from CandidatesMaster where UserId=@cndUserId)    
    
 Insert into Candidate (IRFId, ResumeLink, [Location],Phone, CandidateMasterId, ClientHRAttemptContact, ProfileSource)     
 values((select id from IRF where IRFId=@IRFId), @resumeLink, @prefferedLocation, @textreminder,    
 (select id from CandidatesMaster  where UserId = @cndUserId), @ClientHrContact, @staffingAgenecy)    
    
 --insert candidate added status    
 insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy)     
 values ((select id from IRF where IRFId=@IRFId), @canMasterId, 'Added', GETDATE(), 12, 'System')    
     
End    
    
Else    
    
Begin    
 declare    
 @candidateUserId int = (select id from UserList where EmailId = @candidateEmail)    
  --update Userlist for whatsAppNumber  
  update UserList set WhatsappNumber = @WhatsApp where id=@candidateUserId    
 IF NOT EXISTS (SELECT 1  FROM  CandidatesMaster  WHERE  UserId = @candidateUserId)    
  begin    
   Insert into CandidatesMaster (UserId) values(@candidateUserId)    
  end    
    
 declare    
 @candidateMasterId int = (select id from CandidatesMaster where UserId=@candidateUserId)    
 declare @irfintid int = (select id from IRF where IRFId=@IRFId)    
 IF NOT EXISTS (SELECT 1  FROM  Candidate  WHERE  CandidateMasterId = @candidateMasterId and IRFID= @irfintid)    
  Begin    
   Insert into Candidate (IRFId, ResumeLink, [Location],Phone, CandidateMasterId, ClientHRAttemptContact, ProfileSource)     
   values(@irfintid, @resumeLink, @prefferedLocation,@textreminder, @candidateMasterId, @ClientHrContact, @staffingAgenecy)      
       
   --insert candidate added status    
   insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy)     
   values (@irfintid, @candidateMasterId, 'Added', GETDATE(), 12, 'System')    
  End
 Else    
  Begin    
   Update Candidate set ClientHRAttemptContact =@ClientHrContact,  ProfileSource =@staffingAgenecy   
   where CandidateMasterId = @candidateMasterId and IRFID= @irfintid    
    
   --update UserList set MobileNumber = @textreminder where id=@candidateUserId    
  End    
    
End    
    
--insert into CandidateStatus table    
declare @canUserId int,@canMstid int, @idIrf int;    
    
set @canUserId = (select id from UserList where EmailId = @candidateEmail and idProfile=@profileId)    
set @canMstid =(select id from CandidatesMaster where UserId=@canUserId)    
set @idIrf =(select id from IRF where IRFId=@IRFId)    
    
if(@comfirmationStatus='1')    
 Begin    
  insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy, InterviewId)     
  values (@idIrf, @canMstid, 'Scheduled', GETDATE(), 4, 'System', @InterviewId)    
 End    
    
Else    
 Begin    
  insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy, InterviewId)     
  values (@idIrf, @canMstid, 'Cancelled', GETDATE(), 18, 'System', @InterviewId)    
 End    
    
End    
    
Else    
--it is a interviewer skill channel Id, therefore unapproved interviewer's meeting    
Begin    
    
IF NOT EXISTS (SELECT 1   FROM   UnapprovedInterviewerMeetings    WHERE  candidateEmail = @candidateEmail and interviewDate=@interviewDate and ISCId= @IRFId)    
Begin    
    
INSERT INTO [dbo].[UnapprovedInterviewerMeetings]    
           ([ISCId]    
           ,[interviewDate]    
           ,[interviewSlot]    
           ,[interviewDuration]    
     ,[candidateName]    
           ,[candidateEmail]    
           ,[confirmationStatus]    
           ,[zoomLink]    
           ,[ZoomId]    
           ,[sCreatedBy]    
           ,[dtCreatedOn]    
           )    
     VALUES    
   (@IRFId,    
@interviewDate,    
@slot,    
@interviewDuration,    
@candidateName,    
@candidateEmail,    
@comfirmationStatus,    
@zoomLink,    
@hrRiseBirdAssinged,    
'Clendly',    
GETUTCDATE()    
)    
    
set @InterviewId=SCOPE_IDENTITY()    
set @interviewIdOutput = @InterviewId    
    
    
End    
    
    
ELSE    
    
Begin    
    
set @InterviewId = (select id from UnapprovedInterviewerMeetings where candidateEmail = @candidateEmail and interviewDate=@interviewDate and ISCId= @IRFId)    
set @interviewIdOutput = @InterviewId    
    
 UPDATE [dbo].[UnapprovedInterviewerMeetings]    
   SET [confirmationStatus] = @comfirmationStatus    
   ,[zoomLink]=@zoomLink    
      ,[ZoomId]=@hrRiseBirdAssinged    
           
 WHERE id=@InterviewId    
     
     
     
End    
    
    
if(@comfirmationStatus='1')    
 Begin    
    
  IF NOT EXISTS (SELECT 1   FROM   InterviewerComments    WHERE  InterviewId =@InterviewId)    
  Begin    
   insert into InterviewerComments(InterviewId, MeetingStatus) values ( @InterviewId, 2)    
    
  End    
  Else    
  Begin    
   update InterviewerComments set MeetingStatus=2 where InterviewId=@InterviewId    
  End    
    
    
 End    
    
Else    
 Begin    
    update InterviewerComments set MeetingStatus = null where InterviewId=@InterviewId    
    
 End    
    
End    
    
END    
    
    
  

GO



