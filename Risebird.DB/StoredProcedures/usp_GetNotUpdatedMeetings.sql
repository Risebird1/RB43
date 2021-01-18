/****** Object:  StoredProcedure [dbo].[usp_GetNotUpdatedMeetings]    Script Date: 15-01-2021 4.53.33 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetNotUpdatedMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNotUpdatedMeetings 
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetNotUpdatedMeetings]  

AS  
Begin 
select im.id as imid, im.IRFId, im.interviewDate, im.candidateName, im.candidateEmail, im.interviewer, ic.MeetingStatus, ic.InterviewStatus, im.HRCoordinatorEmailId, ic.InterviewerComment, im.confirmationStatus  from InterviewMeetings im 
left join InterviewComments ic on im.id=ic.InterviewId
left join QCFeedbackReport qcfr on im.id =qcfr.InterviewId
where confirmationStatus=1 and DATEDIFF(DAY, interviewDate, GETDATE())<=7 
and ((interviewer is null) or 
(ic.MeetingStatus is null )
or (ic.InterviewStatus is null ) 
or (ic.InterviewerComment is null))
order by interviewDate desc


End

GO


