IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAllRecommendedIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllRecommendedIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 CREATE PROCEDURE [dbo].[usp_GetAllRecommendedIRF]           
@EmailId varchar(200)
AS            
Begin           
  
  select distinct irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,          
  irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails, irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel,Client.CuratedLink as CuratedLink, irf.IsPaused, irf.ClientName,          
  Client.[Name] as ClientCompName,ulh.EmailId as RisebirdHREmail       
  from irf join RecommednedSkillChannel rsc on irf.id=rsc.IRFId inner join Client on IRF.ClientId = Client.id 
  left join UserList ulh on ulh.id = irf.RisebirdHr        
  where rsc.CreatedBy=@EmailId
        
End 

GO