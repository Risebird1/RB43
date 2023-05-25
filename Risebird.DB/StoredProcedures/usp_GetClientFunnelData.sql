IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientFunnelData'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientFunnelData
END 

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO 

Create PROCEDURE [dbo].[usp_GetClientFunnelData]      
@emailId varchar(100),  
@timeNow datetime  
AS      
Begin     
    
declare @profileId int, @roleId int, @userId int,  @clientId int, @currentDate date;    
  set @profileId = (select id from [Profile] where [Name]='Client')    
  set @userId  = (select id from UserList where EmailId=@emailId and idProfile=@profileId)    
  set @roleId  = (select RoleId from ClientUser where UserId=@userId)    
  set @clientId = (select ClientId from ClientUser where UserId = @userId)  
  set @currentDate = (select cast(@timeNow as date))  
     
    
Begin      
      
if(@roleId=1)      
 Begin      
      
  declare @TotalHRCompletedMeetings table (id int, MeetingStatus int, InterviewStatus int, IsApproved int, IsSelected int, OfferStatus int, JoiningStatus int, QCStatus int);      
  insert into @TotalHRCompletedMeetings select distinct im.id, ic.MeetingStatus, ic.InterviewStatus, ic.IsApproved, ic.IsSelected, ic.OfferStatus, ic.JoiningStatus, qcfr.QCStatus from InterviewMeetings im       
  join IRF on im.IRFId=irf.IRFId join UserList ul on ul.EmailId=im.candidateEmail join CandidatesMaster cm on cm.UserId = ul.id join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id)      
  left join InterviewComments ic on ic.InterviewId=im.id      
  left join QCFeedbackReport qcfr on qcfr.InterviewId =im.id      
  where (ClientHrContact=@emailId  or ca.ClientHRAttemptContact =@emailId) and confirmationStatus=1   
  and ic.MeetingStatus in (4,6,7,8)   
  and ((DATEDIFF(DAY, im.interviewDate, @timeNow)<=30 and @clientId !=147) or (DATEDIFF(DAY, im.interviewDate, @timeNow) <= 120 and @clientId = 147))  
      
   select      
      
   (select count( candidateEmail) from InterviewMeetings im     
   join IRF on im.IRFId=irf.IRFId join UserList ul on ul.EmailId=im.candidateEmail     
   join CandidatesMaster cm on cm.UserId = ul.id     
   join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id)    
   where im.ClientHrContact=@emailId and im.confirmationStatus=1   
   and ((DATEDIFF(DAY, im.interviewDate, @timeNow)<=30 and @clientId !=147) or (DATEDIFF(DAY, im.interviewDate, @timeNow) <= 120 and @clientId = 147)))   
   as TotalCandidates,      
         
   (select count(im.id) from InterviewMeetings im    
   join IRF on im.IRFId=irf.IRFId     
   join UserList ul on ul.EmailId=im.candidateEmail    
   join CandidatesMaster cm on cm.UserId = ul.id     
   join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id) where (im.ClientHrContact=@emailId or ca.ClientHRAttemptContact =@emailId)  
   and im.confirmationStatus=1   
   and ((DATEDIFF(DAY, im.interviewDate, @timeNow)<=30 and @clientId !=147) or (DATEDIFF(DAY, im.interviewDate, @timeNow) <= 120 and @clientId = 147)))   
   as TotalConfirmations,      
      
   (select count(id) from @TotalHRCompletedMeetings) as TotalCompleted,      
      
         
   (select count(id) from @TotalHRCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4)) as TotalRisebirdSelection,      
      
   (select count(id) from @TotalHRCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1) as TotalApproved,      
      
   (select count(id) from @TotalHRCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1 and tcm.IsSelected=1) as TotalFinalSelections,      
      
   (select count(id) from @TotalHRCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1 and tcm.IsSelected=1 and tcm.OfferStatus=1) as TotalOffered,      
      
   (select count(id) from @TotalHRCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1 and tcm.IsSelected=1 and tcm.OfferStatus=1 and tcm.JoiningStatus=1) as TotalJoined      
 End      
else if(@roleId=4)      
 Begin       
        
      
  declare @TotalStaffingCompletedMeetings table (id int, MeetingStatus int, InterviewStatus int, IsApproved int, IsSelected int, OfferStatus int, JoiningStatus int, QCStatus int);      
  insert into @TotalStaffingCompletedMeetings select im.id, ic.MeetingStatus, ic.InterviewStatus, ic.IsApproved, ic.IsSelected, ic.OfferStatus, ic.JoiningStatus, qcfr.QCStatus from InterviewMeetings im       
  join IRF on im.IRFId=irf.IRFId join UserList ul on ul.EmailId=im.candidateEmail join CandidatesMaster cm on cm.UserId = ul.id join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id)      
  left join InterviewComments ic on ic.InterviewId=im.id      
  left join QCFeedbackReport qcfr on qcfr.InterviewId =im.id      
  where staffingAgency=@emailId and confirmationStatus=1 and ic.MeetingStatus in (4,6,7,8)   
  and ((DATEDIFF(DAY, im.interviewDate, @timeNow)<=30 and @clientId !=147) or (DATEDIFF(DAY, im.interviewDate, @timeNow) <= 120 and @clientId = 147))    
      
   select      
      
   (select count( candidateEmail) from InterviewMeetings im join IRF on im.IRFId=irf.IRFId    
   join UserList ul on ul.EmailId=im.candidateEmail     
   join CandidatesMaster cm on cm.UserId = ul.id     
   join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id)    
   where im.staffingAgency=@emailId and im.confirmationStatus=1   
   and ((DATEDIFF(DAY, im.interviewDate, @timeNow)<=30 and @clientId !=147) or (DATEDIFF(DAY, im.interviewDate, @timeNow) <= 120 and @clientId = 147)))   
   as TotalCandidates,      
         
   (select count(im.id) from InterviewMeetings im     
   join IRF on im.IRFId=irf.IRFId    
   join UserList ul on ul.EmailId=im.candidateEmail     
   join CandidatesMaster cm on cm.UserId = ul.id     
   join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id)    
   where staffingAgency=@emailId and confirmationStatus=1   
   and ((DATEDIFF(DAY, im.interviewDate, @timeNow)<=30 and @clientId !=147) or (DATEDIFF(DAY, im.interviewDate, @timeNow) <= 120 and @clientId = 147)))   
   as TotalConfirmations,      
      
   (select count(id) from @TotalStaffingCompletedMeetings) as TotalCompleted,      
      
         
   (select count(id) from @TotalStaffingCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4)) as TotalRisebirdSelection,      
      
   (select count(id) from @TotalStaffingCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1) as TotalApproved,      
      
   (select count(id) from @TotalStaffingCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1 and tcm.IsSelected=1) as TotalFinalSelections,      
      
   (select count(id) from @TotalStaffingCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1 and tcm.IsSelected=1 and tcm.OfferStatus=1) as TotalOffered,      
      
   (select count(id) from @TotalStaffingCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1 and tcm.IsSelected=1 and tcm.OfferStatus=1 and tcm.JoiningStatus=1) as TotalJoined      
 End      
else      
 Begin      
         
   declare @listofIRFIds table (id int);      
   insert into @listofIRFIds select irf.id from IRF join TransClientUser tcu on tcu.IRFId = IRF.id where tcu.ClientUserId = @userId      
      
   declare @listofIRF table (id varchar(100));      
   insert into @listofIRF select irf.IRFId from IRF join TransClientUser tcu on tcu.IRFId = IRF.id where tcu.ClientUserId = @userId      
      
         
   declare @TotalCompletedMeetings table (id int, MeetingStatus int, InterviewStatus int, IsApproved int, IsSelected int, OfferStatus int, JoiningStatus int, QCStatus int);      
     insert into @TotalCompletedMeetings select im.id, ic.MeetingStatus, ic.InterviewStatus, ic.IsApproved, ic.IsSelected, ic.OfferStatus, ic.JoiningStatus, qcfr.QCStatus from InterviewMeetings im       
     join IRF on im.IRFId=irf.IRFId join UserList ul on ul.EmailId=im.candidateEmail join CandidatesMaster cm on cm.UserId = ul.id join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id)      
     left join InterviewComments ic on ic.InterviewId=im.id      
     left join QCFeedbackReport qcfr on qcfr.InterviewId =im.id      
     where im.IRFId in (select id from @listofIRF) and confirmationStatus=1 and ic.MeetingStatus in (4,6,7,8)     
 and ((DATEDIFF(DAY, im.interviewDate, @timeNow)<=30 and @clientId !=147) or (DATEDIFF(DAY, im.interviewDate, @timeNow) <= 120 and @clientId = 147))    
   select      
      
         
  (select count( distinct cm.UserId) from TransClientUser tcu join irf on irf.id= tcu.IRFId      
    join Candidate ca on ca.IRFId=irf.id     
 join CandidatesMaster cm on cm.id=ca.CandidateMasterId      
 join UserList ul on ul.id = cm.UserId    
 join InterviewMeetings  im on im.candidateEmail = ul.EmailId    
    where tcu.ClientUserId=@userId   
 and ((DATEDIFF(DAY, im.interviewDate, @timeNow)<=30 and @clientId !=147) or (DATEDIFF(DAY, im.interviewDate, @timeNow) <= 120 and @clientId = 147)))  
 as TotalCandidates,      
         
   (select count(distinct im.id) from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join UserList ul on ul.EmailId=im.candidateEmail join CandidatesMaster cm on cm.UserId = ul.id   
   join Candidate ca on (ca.CandidateMasterId= cm.id and ca.IRFId=irf.id)   
   where im.IRFId in (select id from @listofIRF) and confirmationStatus=1   
   and ((DATEDIFF(DAY, im.interviewDate, @timeNow)<=30 and @clientId !=147) or (DATEDIFF(DAY, im.interviewDate, @timeNow) <= 120 and @clientId = 147)))   
   as TotalConfirmations,      
      
   (select count(distinct id) from @TotalCompletedMeetings) as TotalCompleted,      
      
   (select count(distinct id) from @TotalCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4)) as TotalRisebirdSelection,      
      
   (select count(distinct id) from @TotalCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1) as TotalApproved,      
      
   (select count(distinct id) from @TotalCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1 and tcm.IsSelected=1) as TotalFinalSelections,      
      
   (select count(distinct id) from @TotalCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1 and tcm.IsSelected=1 and tcm.OfferStatus=1) as TotalOffered,      
      
   (select count(distinct id) from @TotalCompletedMeetings tcm where (tcm.InterviewStatus=2 or tcm.InterviewStatus=4) and tcm.IsApproved=1 and tcm.IsSelected=1 and tcm.OfferStatus=1 and tcm.JoiningStatus=1) as TotalJoined      
      
 End      
      
End   
    
End
  
    

Go
  
  