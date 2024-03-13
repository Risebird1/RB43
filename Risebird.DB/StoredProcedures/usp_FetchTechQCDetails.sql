IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchTechQCDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchTechQCDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchTechQCDetails
AS
BEGIN
Select tqd.Interviewid,im.IRFId,im.ConfirmationStatus,im.ClientHrContact,im.candidateEmail,ic.MeetingStatus,ic.InterviewStatus,im.Interviewer,cl.Name as Client,IRF.JDTiltle,im.interviewdate,ul.Emailid as ChannelOwner 
From TechQualityDetails tqd join InterviewMeetings im on tqd.Interviewid=im.id
join InterviewComments ic on ic.Interviewid=im.id
join IRF on IRF.IRFId=im.IRFId
join Client cl on cl.id=IRF.Clientid
left join UserList ul on ul.id=IRF.Risebirdhr
where tqd.IsActive=1
END
Go