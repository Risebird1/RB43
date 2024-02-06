  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetMoneyProtectionDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMoneyProtectionDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetMoneyProtectionDetails  
@type varchar(25),
@Clientid int
AS
BEGIN
if(@type='Pending')
BEGIN
Select Cl.id,Cl.Name as ClientName,mp.Points as Point,cl.BillingType,mp.AddedOn,im.interviewDate,im.candidateEmail,im.IRFId,IRF.Heuristicsscore,cl.PerInterviewRate,ps.PaymentComplianceCount,im.id, im.ClientHrContact From MoneyProtectionWallet mp join Client cl on cl.id=mp.Clientid
left join InterviewMeetings im on im.id=mp.Interviewid left join IRF on IRF.IRFId=im.IRFId left join PaymentSLADetails ps on ps.Interviewid=im.id
where ISNULL(mp.IsConverted,0)=0 and mp.Clientid=@Clientid
END
else if(@type='Pending2')
BEGIN
Select Cl.id,Cl.Name as ClientName,mp.Points as Point,cl.BillingType,mp.AddedOn,im.interviewDate,im.candidateEmail,im.IRFId,IRF.Heuristicsscore,cl.PerInterviewRate,ps.PaymentComplianceCount,im.id, im.ClientHrContact From MoneyProtectionWallet mp join Client cl on cl.id=mp.Clientid
left join InterviewMeetings im on im.id=mp.Interviewid left join IRF on IRF.IRFId=im.IRFId left join PaymentSLADetails ps on ps.Interviewid=im.id
where ISNULL(mp.IsConverted,0)=0 and mp.Clientid=@Clientid and ISNULL(cl.Reportsnippet,0)>0
END
ELSE
BEGIN
Select Cl.id,Cl.Name as ClientName,mp.Points as Point,cl.BillingType,mp.AddedOn,im.interviewDate,im.candidateEmail,im.IRFId,IRF.Heuristicsscore, ps.PaymentComplianceCount,im.id,im.ClientHrContact From MoneyProtectionWallet mp join Client cl on cl.id=mp.Clientid
left join InterviewMeetings im on im.id=mp.Interviewid left join IRF on IRF.IRFId=im.IRFId
left join PaymentSLADetails ps on ps.Interviewid=im.id
where ISNULL(mp.IsConverted,0)=1 and mp.Clientid=@Clientid
END
End
GO