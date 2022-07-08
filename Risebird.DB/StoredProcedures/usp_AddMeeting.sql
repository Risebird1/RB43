-- =============================================
-- SP Name:  dbo.usp_AddMeeting
-- Author: Prem     
-- Create date: 7th July 2019
-- Description: Add Meeting
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddMeeting'))
BEGIN
    DROP PROCEDURE dbo.usp_AddMeeting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddMeeting]  
@IRFId varchar(200),  
@interviewDate varchar(200),  
@interviewSlot varchar(100),  
@interviewDuration int,  
@candidateName varchar(200),  
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
@currentEmployerName varchar(200),  
@MobileNumber nvarchar(200),  
--@QCInterviewer varchar(200),  
@zoomMeetingId varchar(200) 
AS  
Begin  
  
declare @InterviewId int;   
  
  
declare  @irfIntId int  
     
   set @irfIntId =(select id from IRF where IRFId=@IRFId)  
  
  
IF NOT EXISTS (SELECT 1   FROM   InterviewMeetings    WHERE  candidateEmail = @candidateEmail and interviewDate=@interviewDate)  
  
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
      ,[interviewer]  
      ,[risebirdHR]  
      ,[PreferredLocation]  
      ,[ExpectedPerHourRate]  
      ,[joiningTime]  
      ,[staffingAgency]  
      ,[currentEmployerName]       
      ,[sCreatedBy]  
      ,[dtCreatedOn]  
      ,[MobileNumber]  
      ,[ZoomMeetingId]  
      )  
   VALUES  
      (@IRFId,  
 @interviewDate,  
 @interviewSlot,  
 @interviewDuration,  
 @candidateName,  
 @candidateEmail,  
 @ClientHrContact,  
 @resumeLink,  
 @comfirmationStatus,  
 @zoomLink,  
 @interviewer,  
 @hrRiseBirdAssinged,  
 @preferredLocation,  
 @expectedPerHourRate,  
 @joiningTime,  
 @staffingAgency,  
 @currentEmployerName,  
 'System',  
 GETUTCDATE(),  
 @MobileNumber,  
 @zoomMeetingId  
 )  
  
 set @InterviewId=SCOPE_IDENTITY()  
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
   
 Insert into UserList (idProfile, FirstName, EmailId, IsActive,MobileNumber,uniqueid) values (@profileId, @candidateName, @candidateEmail, 1,@MobileNumber,NEWID())  
  
 declare   
 @cndUserId int = (select id from UserList where EmailId=@candidateEmail and idProfile=@profileId)  
  
 Insert into CandidatesMaster (UserId) values(@cndUserId)  
 declare  
 @canMasterId int = (select id from CandidatesMaster where UserId=@cndUserId)  
  
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
  
 declare  
 @candidateMasterId int = (select id from CandidatesMaster where UserId=@candidateUserId)  
 --declare @irfintid int = (select id from IRF where IRFId=@IRFId)  
 IF NOT EXISTS (SELECT 1  FROM  Candidate  WHERE  CandidateMasterId = @candidateMasterId and IRFID= @irfintid)  
  Begin  
   Insert into Candidate (IRFId, ResumeLink, [Location],Phone, CandidateMasterId, ClientHRAttemptContact, ProfileSource)   
   values(@irfintid, @resumeLink, @preferredLocation,@MobileNumber, @candidateMasterId, @ClientHrContact, @staffingAgency)    
     
   --insert candidate added status  
   insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy)   
   values (@irfintid, @candidateMasterId, 'Added', GETDATE(), 12, 'System')  
  End  
  
End  
  
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
  
  
END
GO



