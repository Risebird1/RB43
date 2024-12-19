IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientRemovedInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientRemovedInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetClientRemovedInterviewers  
AS  
BEGIN  
Select IRF.IRFId,ul.EmailId as interviewer,tr.IsActive,cl.Name as Client,ulc.EmailId as ChannelOwner,IRF.JDTiltle,ulb.EmailId as Clienthr,tci.TotalCompletedMeetings,tsi.TotalSelected,tri.TotalRejected,IRF.id   
from TransInterviewer tr join IRF on IRF.id=tr.IRFId  
join UserList ul on ul.Id=tr.UserId  
join Client cl on cl.id=IRF.ClientId  
left join UserList ulc on ulc.id=IRF.RisebirdHr  
left join UserList ulb on ulb.id=IRF.BillingOwner  
outer apply(Select Count(im.id) as TotalCompletedMeetings from InterviewMeetings im left join InterviewComments ic on ic.InterviewId = im.id where im.IRFId=IRF.IRFId and im.interviewer=ul.EmailId and (ic.MeetingStatus in (4, 6, 7, 8)) and im.IRFId=IRF.IRFId) as tci  
outer apply(Select Count(im.id) as TotalSelected from InterviewMeetings im left join InterviewComments ic on ic.InterviewId = im.id where  im.IRFId=IRF.IRFId and im.interviewer=ul.EmailId and (ic.InterviewStatus = 2 or ic.InterviewStatus = 4) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 6 or ic.MeetingStatus = 8)) as tsi  
outer apply(Select Count(im.id) as TotalRejected from InterviewMeetings im left join InterviewComments ic on ic.InterviewId = im.id where  im.IRFId=IRF.IRFId and im.interviewer=ul.EmailId and (ic.InterviewStatus = 3 or ic.InterviewStatus = 5 or ic.InterviewStatus = 1) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 6 or ic.MeetingStatus = 8)) as tri  
where ClientAction='Rejected'  
END
GO