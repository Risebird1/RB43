IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchClonableTempStatsDetailsbyId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchClonableTempStatsDetailsbyId
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_FetchClonableTempStatsDetailsbyId  
@id int  
AS  
BEGIN  
Select IRF.id,IRF.IRFId,cl.Name as ClientName,IRF.JDTiltle,ag.AssessmentGroup as AssessmentGroupName ,Count(im.id) as InterviewCounts from IRF  
join Client cl on IRF.ClientId=cl.id  
left join AssessmentGroup ag on ag.id=IRF.AssessmentGroupId  
Left join InterviewMeetings im on IRF.IRFId=im.IRFId  
where CreatedFromTemplateId=@id  
GROUP BY IRF.id,IRF.IRFId,cl.Name,IRF.JDTiltle,ag.AssessmentGroup  
END  
GO  