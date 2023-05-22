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
@countryType int = null
AS          
Begin  
declare @rate int

set @rate = (select Rate from PlanTypeChargesMaster where CountryType = @countryType and PlanType = @planType)

select @rate

End 
GO

