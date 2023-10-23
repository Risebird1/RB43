/****** Object:  StoredProcedure [dbo].[usp_GetInterviewReportById]    Script Date: 11-09-2019 8.39.23 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewReportById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewReportById
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewReportById]  
@InterviewId int,  
@IrfId int  
AS  
Begin  
select ip.*, ic.*, IRF.JobLevel,IRF.IRFId as IRFName, irf.IsBiometricEnabled, im.resumeLink, im.interviewDate,im.currentEmployerName,   
im.JoiningBiometryScore, im.InterviewBiometryScore, irf.IsQCRequired,
Case When cl.Reportsnippet=1 or Reportsnippet=2 then 0 Else 1 end as ReportAccepted
from InterviewReport ip  
inner join IRF on ip.IRFId = IRF.id   
inner join [InterviewComments] ic on ic.InterviewId = ip.InterviewId  
inner join [InterviewMeetings] im on im.id = ip.InterviewId 
left join Client cl on cl.id=IRF.ClientId
where ip.InterviewId=@InterviewId and ip.IRFId=@IrfId  
End  
GO


