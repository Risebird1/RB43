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
Select Cl.id,Cl.Name as ClientName,mp.Points as Point,cl.BillingType,mp.AddedOn,im.interviewDate,im.candidateEmail,im.IRFId From MoneyProtectionWallet mp join Client cl on cl.id=mp.Clientid
left join InterviewMeetings im on im.id=mp.Interviewid
where ISNULL(mp.IsConverted,0)=0 and mp.Clientid=@Clientid
END
ELSE
BEGIN
Select Cl.id,Cl.Name as ClientName,mp.Points as Point,cl.BillingType,mp.AddedOn,im.interviewDate,im.candidateEmail,im.IRFId From MoneyProtectionWallet mp join Client cl on cl.id=mp.Clientid
left join InterviewMeetings im on im.id=mp.Interviewid
where ISNULL(mp.IsConverted,0)=1 and mp.Clientid=@Clientid
END
End
GO