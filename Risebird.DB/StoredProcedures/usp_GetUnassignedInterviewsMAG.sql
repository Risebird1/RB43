
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

select distinct mag.AssessmentGroup as MasterAG, ag.AssessmentGroup, Mag.id, Mag.MAGLinkEmailTriggeredBy, Mag.MAGLinkEmailTriggeredOn, AGWiseCount.NoOfUnassigned 
from InterviewMeetings im
join IRF on irf.irfid = im.irfid
join AssessmentGroup ag on ag.id = irf.AssessmentGroupId
outer apply (select aginner.AssessmentGroup, aginner.id, maginner.MAGLinkEmailTriggeredBy, maginner.MAGLinkEmailTriggeredOn from MasterAssessmentGroup maginner 
join AssessmentGroup aginner on aginner.id = maginner.AssessmentGroupId where AssessmentGroupId = irf.AssessmentGroupId) Mag
outer apply (select count(imop.id) as NoOfUnassigned from InterviewMeetings imop join irf irfop on irfop.irfId= imop.IRFId join AssessmentGroup agop 
on agop.id = irfop.AssessmentGroupId where agop.id = Mag.id and imop.interviewDate >= dateadd(MINUTE, 390, GETDATE()) and imop.confirmationStatus = 1
and interviewer is null and Mag.id is not null) AGWiseCount
where interviewDate >= dateadd(MINUTE, 390, GETDATE()) and confirmationStatus = 1
and interviewer is null and Mag.id is not null
order by NoOfUnassigned desc

End
Go

