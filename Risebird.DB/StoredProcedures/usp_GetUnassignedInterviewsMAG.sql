
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetUnassignedInterviewsMAG'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUnassignedInterviewsMAG
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetUnassignedInterviewsMAG]               
@timeNow datetime                 
AS                
BEGIN

select distinct mag.AssessmentGroup as MasterAG, ag.AssessmentGroup, Mag.id, Mag.MAGLinkEmailTriggeredBy, Mag.MAGLinkEmailTriggeredOn 
from InterviewMeetings im
join IRF on irf.irfid = im.irfid
join AssessmentGroup ag on ag.id = irf.AssessmentGroupId
outer apply (select ag.AssessmentGroup, ag.id, mag.MAGLinkEmailTriggeredBy, mag.MAGLinkEmailTriggeredOn from MasterAssessmentGroup mag 
join AssessmentGroup ag on ag.id = mag.AssessmentGroupId where AssessmentGroupId = irf.AssessmentGroupId) Mag
where interviewDate >= dateadd(MINUTE, 390, GETDATE()) and confirmationStatus = 1
and interviewer is null and Mag.id is not null

End
Go

