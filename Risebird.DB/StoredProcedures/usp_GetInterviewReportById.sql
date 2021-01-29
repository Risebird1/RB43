/****** Object:  StoredProcedure [dbo].[usp_GetInterviewReportById]    Script Date: 11-09-2019 8.39.23 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewReportById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewReportById
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerReportById]  
  
@InterviewId int,  
@IrfId int  
  
  
AS  
Begin  
  
  
select ir.*, ic.*, isc.InterviewerSkillId as IRFName, im.resumeLink, im.interviewDate,im.currentEmployerName, im.JoiningBiometryScore, im.InterviewBiometryScore from InterviewerReport ir  
inner join InterviewerSkillsChannel as isc on ir.InterviewerSkillId = isc.id   
inner join [InterviewerComments] ic on ic.InterviewId = ir.InterviewId  
inner join [InterviewMeetings] im on im.id = ir.InterviewId  
where ir.InterviewId=@InterviewId and ir.InterviewerSkillId=@IrfId  
  
End 

GO


