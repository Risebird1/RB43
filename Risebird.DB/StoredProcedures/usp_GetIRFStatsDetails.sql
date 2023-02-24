IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIRFStatsDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFStatsDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO                        
CREATE PROCEDURE usp_GetIRFStatsDetails
@irfid int,    
@type varchar(50)   
AS            
BEGIN       
if(@type='MissingZoom')
begin
select im.IRFId,im.interviewdate,im.CandidateEmail,im.CandidateName,im.ClientHrContact as ClientHRAttemptContact,cl.Name as ClientName from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.Clientid              
where IRF.id=@irfid and confirmationStatus=1 and (zoomLink is null or zoomLink='')
End
else if(@type='MissingVideo')
begin
select im.IRFId,im.interviewdate,im.CandidateEmail,im.CandidateName,im.ClientHrContact as ClientHRAttemptContact,cl.Name as ClientName from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.Clientid             
where IRF.id=@irfid and ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='')
End
else if(@type='StatusPending')
begin
select im.IRFId,im.interviewdate,im.CandidateEmail,im.CandidateName,im.ClientHrContact as ClientHRAttemptContact,cl.Name as ClientName 
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId left join InterviewComments ic on ic.InterviewId= im.id join Client cl on cl.id=IRF.Clientid            
where IRF.id=@irfid and confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2)
End
else if(@type='UnassignedInterview')
begin
select im.IRFId,im.interviewdate,im.CandidateEmail,im.CandidateName,im.ClientHrContact as ClientHRAttemptContact,cl.Name as ClientName from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.Clientid              
 where IRF.id=@irfid  and confirmationStatus=1 and interviewer is null
End
END          
GO 