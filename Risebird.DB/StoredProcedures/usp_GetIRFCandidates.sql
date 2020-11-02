/****** Object:  StoredProcedure [dbo].[usp_GetIRFCandidates]    Script Date: 10-06-2020 9.24.16 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFCandidates
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetIRFCandidates]

@IRFId int

AS
Begin

select Cnd.*, ims.confirmationStatus, ims.interviewDate, ims.MeetingStatus, ims.InterviewStatus, ims.VideoLink, ims.imid from 
(select ca.id, ca.IRFId, ca.ResumeLink,ca.TotalInvites, ca.[Type], ca.[Location], ca.Skill, ca.WorkAuth, ca.CurrentLocation, ca.CurrentEmployer,
ca.TotalExperience, ca.CurrentSalary, ca.ExpectedSalary, ca.Notice, ca.SelectabilityComments,ca.Heuristicsscore,
ca.JoinabilityComments, ca.JoinabilityScore, ca.SelectabilityScore, ca.TechSkillComments, ca.SkillGeneralComments,ca.CostEstimate,
ca.TechSkillScore, ca.SkillGeneralScore, ca.AddedByUserId, ca.VoiceItUserId, ca.InviteDate, ca.[Status], ul.EmailId, ca.Phone,
ul.FirstName as [Name], IRF.EndDate, IRF.IRFId as IRFName,cm.id as CandidateMasterId ,Isnull(ca.EncodedResumeLink,'') as EncodedResumeLink, ca.AvailableFullTime, ca.AvailableFreeLancing
from Candidate ca join IRF on ca.IRFId=IRF.id
join CandidatesMaster cm on ca.CandidateMasterId=cm.id  
join UserList ul on ul.id = cm.UserId
where ca.IRFId = @IRFId) Cnd  
left join (select im.id as imid,im.interviewDate, im.confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,
ic.VideoLink, im.candidateEmail, im.IRFId from InterviewMeetings im join
(select candidateEmail, max(interviewDate) as intereviewDate from InterviewMeetings group by candidateEmail) Filtered
on im.candidateEmail= Filtered.candidateEmail and im.interviewDate=Filtered.intereviewDate
left join InterviewComments ic on ic.InterviewId = im.id) ims on ims.candidateEmail= Cnd.EmailId and Cnd.IRFName = ims.IRFId
End








GO


