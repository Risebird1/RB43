/****** Object:  StoredProcedure [dbo].[usp_GetUserId]    Script Date: 08-01-2020 7.40.41 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ComingDaysIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_ComingDaysIRF
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ComingDaysIRF]
AS
Begin
Declare @endDate as date
 SELECT @endDate= dateadd(dd,6,getdate())
Select IRF.IRFId,IRF.ClientName,IRF.JDTiltle, Client.[Name] as ClientCompName from IRF inner join Client on IRF.ClientId = Client.id  
where IRF.IsDeleted= 0 and IRF.EndDate>=@endDate
order by IRF.EndDate asc

END

GO


