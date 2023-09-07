IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPaymentSlacompliance'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPaymentSlacompliance
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE usp_GetPaymentSlacompliance
AS
BEGIN
Select ps.*,IRF.IRFId,IRF.id as irfIntId,im.candidateEmail,im.interviewer,im.interviewDate,ul.EmailId as ChannelOwner,ip.CompanyName,uli.MobileNumber,RTRIM(LTRIM((ISNULL(uli.FirstName,'')+' '+ISNULL(uli.LastName,'')))) as InterviewerName
From PaymentSLADetails ps left join InterviewMeetings im on ps.Interviewid=im.id 
left join IRF on IRF.IRFId=im.IRFId 
left join UserList ul on ul.id=IRF.RisebirdHr
left join UserList uli on uli.EmailId=im.interviewer
left join InterviewerProfile ip on ip.UserId=uli.id
where ISNull(ps.IsApproved,0)=0
END
GO