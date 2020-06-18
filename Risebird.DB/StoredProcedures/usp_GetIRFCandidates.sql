/****** Object:  StoredProcedure [dbo].[usp_GetIRFCandidates]    Script Date: 10-06-2020 9.24.16 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFCandidates
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetIRFCandidates]

@IRFId int

AS
Begin

select Cnd.*, ims.confirmationStatus, ims.interviewDate, ims.MeetingStatus, ims.InterviewStatus, ims.VideoLink, ims.imid from 
(select ca.*,IRF.EndDate, IRF.IRFId as IRFName,cm.id as CandidateMasterId 
from Candidate ca join IRF on ca.IRFId=IRF.id join CandidatesMaster cm on ca.EmailId=cm.EmailId 
where ca.IRFId = @IRFId) Cnd 
left join (select im.id as imid,im.interviewDate, im.confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,
ic.VideoLink, im.candidateEmail, im.IRFId from InterviewMeetings im join
(select candidateEmail, max(interviewDate) as intereviewDate from InterviewMeetings group by candidateEmail) Filtered
on im.candidateEmail= Filtered.candidateEmail and im.interviewDate=Filtered.intereviewDate
left join InterviewComments ic on ic.InterviewId = im.id) ims on ims.candidateEmail= Cnd.EmailId and Cnd.IRFName = ims.IRFId
End


GO


