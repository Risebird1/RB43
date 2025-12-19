IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.Usp_GetClientWalletBalance'))
BEGIN
    DROP PROCEDURE dbo.Usp_GetClientWalletBalance
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure Usp_GetClientWalletBalance
@BillingType int,
@ClientId int,
@BillingOwnerId int
AS
BEGIN
Declare @ClientRoleId int  
Select @ClientRoleId=RoleId From ClientUser where UserId=@BillingOwnerId  
if(@BillingType=1)
begin
SELECT ISNULL(SUM(Credit) - SUM(Debit), 0) as Balance 
FROM Wallet 
WHERE BillingType = @BillingType AND (BillingOwnerId = @BillingOwnerId OR @ClientRoleId=9)
AND (@ClientId IS NULL OR ClientId = @ClientId)    
end
else if(@BillingType=3)
begin
SELECT ISNULL(SUM(Credit) - SUM(Debit), 0) as Balance FROM Wallet WHERE BillingType = @BillingType AND ClientId = @ClientId
end
END
Go