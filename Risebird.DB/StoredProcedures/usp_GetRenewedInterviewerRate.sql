IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRenewedInterviewerRate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRenewedInterviewerRate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetRenewedInterviewerRate]          
@planType varchar(25) = null,
@countryType int = null,
@irfId int  = null
AS          
Begin  
declare @rate int

if(@irfId is not null)
Begin
	set @rate = (select Rate from PlanTypeChargesMaster where CountryType = (select JobCountry from IRF where id = @irfId) 
	and PlanTypeEnumValue = (select Heuristicsscore from IRF where id = @irfId))

End
Else
Begin
set @rate = (select Rate from PlanTypeChargesMaster where CountryType = @countryType and PlanType = @planType)
End

select @rate

End 
GO

