IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_RedmarkerInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_RedmarkerInterviewers
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_RedmarkerInterviewers
AS
BEGIN
Select ul.EmailId as interviewer,ul.MobileNumber,ip.NickName,ip.ResumeLink,ip.CompanyName,trm.RedMarker,trm.IRFId as id,IRF.IRFId,ul.id as UserId,IRF.JDTiltle,cl.Name as Client,
ulc.EmailId as ChannelOwner,ulb.EmailId as Clienthr
From TransInterviewerRedMarker trm join InterviewerProfile ip on trm.Interviewerid=ip.Id 
join IRF on IRF.id=trm.IRFId
join UserList ul on ul.id=ip.UserId
left join Client cl on cl.id=IRF.ClientId
left join UserList ulc on ulc.id=IRF.RisebirdHr
left join UserList ulb on ulb.id=IRF.BillingOwner 
where trm.RedMarker>0
END
GO