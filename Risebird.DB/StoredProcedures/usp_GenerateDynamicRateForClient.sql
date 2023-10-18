  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GenerateDynamicRateForClient'))
BEGIN
    DROP PROCEDURE dbo.usp_GenerateDynamicRateForClient
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GenerateDynamicRateForClient  
@margin int,
@clientId int
AS
BEGIN

declare @standardPlanRate int = 0, @proPlanRate int = 0, @advancedPlanRate int = 0

declare @clientCompanyType int = 0, @clientCountryType int 

set @clientCompanyType = (select ClientType from Client where id = @clientId)

set @clientCountryType = (select CountryType from Client where id = @clientId)


set @standardPlanRate = (select rate from PlanTypeChargesMaster where CountryType = @clientCountryType and PlanTypeEnumValue = 2 and CompanyType = @clientCompanyType)

set @proPlanRate = (select rate from PlanTypeChargesMaster where CountryType = @clientCountryType and PlanTypeEnumValue = 4 and CompanyType = @clientCompanyType)

set @advancedPlanRate = (select rate from PlanTypeChargesMaster where CountryType = @clientCountryType and PlanTypeEnumValue = 5 and CompanyType = @clientCompanyType)



select @standardPlanRate * (1 + @margin)/100 as StandardPlanRate, 
	   @proPlanRate * (1 + @margin)/100 as ProPlanRate,
	   @advancedPlanRate * (1 + @margin)/100 as AdvancedPlanRate
End
GO

