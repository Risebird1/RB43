/****** Object:  StoredProcedure [dbo].[usp_AddIRFDetails]    Script Date: 26-09-2019 5.05.16 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddIRFDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddIRFDetails
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddIRFDetails]

@IRFId nvarchar(200),
@clientName varchar(200),
@JDTiltle nvarchar(200),
@CompanyType varchar(200),
@ClientId int,
@JobCountry varchar(50),
@StartDate datetime,
@EndDate datetime,
@JDLink varchar(200),
@NoOfPosition int,
@NoOfIntterview int,
@RisebirdHr int,
@PerMintuteRate real,
@JobLevel int,
@CalendlyLink varchar(500),
@TotalBudget int,
@idIrf int output

AS
Begin

INSERT INTO [dbo].[IRF]
           ([IRFId]
           ,[clientName]
           ,[JDTiltle]
           ,[CompanyType]
		   ,[ClientId]
           ,[JobCountry]
           ,[StartDate]
           ,[EndDate]           
           ,[JDLink]
           ,[NoOfPosition]
           ,[NoOfIntterview]
           ,[RisebirdHr]
           ,[PerMintuteRate]  
		   ,[JobLevel]
		   ,[CalendlyLink]
		   ,[TotalBudget]
		   )
       VALUES
           (@IRFId,
@clientName,
@JDTiltle,
@CompanyType,
@ClientId,
@JobCountry,
@StartDate,
@EndDate,
@JDLink,
@NoOfPosition,
@NoOfIntterview,
@RisebirdHr,
@PerMintuteRate,
@JobLevel,
@CalendlyLink,
@TotalBudget)


End




GO


