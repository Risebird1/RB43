/****** Object:  StoredProcedure [dbo].[usp_GetIRFProgress]    Script Date: 29-07-2020 10.46.48 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFProgress'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFProgress
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_GetIRFProgress]  
@dateValue datetime  
  
AS    
Begin   
  
select irf.id, irf.IRFId, JDTiltle, irf.idProgress, ipr.[Name] as [ProgressStatus], irf.Comment,  
(irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,Ias.Status as AssignStatus,ul.EmailId as AssignTo  from IRF  
join IrfProgress ipr on ipr.id = irf.idProgress 
left Join IRFActivityStatus Ias on Ias.IRFId=IRF.id and Ias.idProgress=ipr.id left join userlist ul on ul.id=Ias.Userid
where cast(irf.EndDate as date) >= cast (@dateValue as date) and cast(irf.StartDate as date) <= cast (@dateValue as date) order by idProgress asc

  
  
  
End  
  

GO


