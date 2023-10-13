  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetMoneyProtectionList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMoneyProtectionList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetMoneyProtectionList
@type varchar(25)
AS
BEGIN
if(@type='Pending')
BEGIN
Select Cl.id,Cl.Name as ClientName,SUM(mp.Points) as Point,cl.BillingType From MoneyProtectionWallet mp join Client cl on cl.id=mp.Clientid
where ISNULL(mp.IsConverted,0)=0
group by Cl.id,Cl.Name,cl.BillingType
END
ELSE
BEGIN
Select Cl.id,Cl.Name as ClientName,SUM(mp.Points) as Point,cl.BillingType From MoneyProtectionWallet mp join Client cl on cl.id=mp.Clientid
where ISNULL(mp.IsConverted,0)=1
group by Cl.id,Cl.Name,cl.BillingType
END
End
GO