/****** Object:  StoredProcedure [dbo].[usp_GetIRFInterviewerMembers]    Script Date: 04-09-2020 12.06.07 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SkillChannelsList'))
BEGIN
    DROP PROCEDURE dbo.usp_SkillChannelsList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_SkillChannelsList    
AS    
BEGIN    
Select IRF.id,IRF.IRFId,IRF.SkillSetsevaluate,IRF.Clientid, Client.[Name] as ClientName,    
Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,    
IRF.IsPaused,IRF.EndDate,IRF.IsDeleted,ti.TotalInterviewers,tai.ActiveInterviewers,tii.InActiveInterviewers,IRF.JobCountry,IRF.CompanyType,IRF.Heuristicsscore    
from IRF inner join Client on IRF.ClientId = Client.id     
outer Apply (Select Count(id) TotalInterviewers From TransInterviewer where IRFId=IRF.id) ti    
outer Apply (Select Count(id) ActiveInterviewers From TransInterviewer where IRFId=IRF.id and IsActive=1) tai    
outer Apply (Select Count(id) InActiveInterviewers From TransInterviewer where IRFId=IRF.id and IsActive=0) tii    
where IRF.Clientid=177    
order by IRF.StartDate desc    
END
GO