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
@IRFId int,
@emailId varchar(50) = null
AS  
Begin    
declare @IRFName varchar(100), @roleId int
set @IRFName = (select IRFId from IRF where id =@IRFId)
declare @tblAttemptStatusCount table (IRFId int, CandidateId int, AttemptsCount int);    
 insert into @tblAttemptStatusCount    
 select IRFId, CandidateId, count(id) from CandidateStatus where IRFId = @IRFId group by IRFId, CandidateId     

if(@emailId is not null)
begin
	declare @profile varchar(150)
	set @profile = (select pr.[Name] from UserList ul join [Profile] pr on pr.id = ul.idProfile where ul.EmailId=@emailId)
	if(@profile =  'Client')
	begin
		set @roleId = (select cu.RoleId from UserList ul join ClientUser cu on cu.UserId = ul.id where ul.EmailId = @emailId)
	end
end

if(@roleId = 1) --client HR
begin
select Cnd.*, ims.confirmationStatus, ims.interviewDate, ims.MeetingStatus, ims.InterviewStatus, ims.VideoLink, ims.imid from     
(select ca.id, ca.IRFId, ca.ResumeLink,ca.TotalInvites, ca.[Type], ca.[Location], ca.Skill, ca.WorkAuth, ca.CurrentLocation, ca.CurrentEmployer,    
ca.TotalExperience, ca.CurrentSalary, ca.ExpectedSalary, ca.Notice, ca.SelectabilityComments,ca.Heuristicsscore,    
ca.JoinabilityComments, ca.JoinabilityScore, ca.SelectabilityScore, ca.TechSkillComments, ca.SkillGeneralComments,ca.CostEstimate,    
ca.TechSkillScore, ca.SkillGeneralScore, ca.AddedByUserId, ca.VoiceItUserId, ca.InviteDate, ca.[Status], ul.EmailId, ca.Phone,    
case 
	 when ul.LastName is null then ul.FirstName
	 else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
end as [Name],
IRF.EndDate, IRF.IRFId as IRFName,cm.id as CandidateMasterId ,Isnull(ca.EncodedResumeLink,'') as EncodedResumeLink, ca.AvailableFullTime, ca.AvailableFreeLancing,    
tasc.AttemptsCount, ca.ClientHRAttemptContact, ca.ProfileSource,ul.WhatsappNumber,ca.IsProctor    
from Candidate ca join IRF on ca.IRFId=IRF.id    
join CandidatesMaster cm on ca.CandidateMasterId=cm.id      
join UserList ul on ul.id = cm.UserId
left join @tblAttemptStatusCount tasc on tasc.CandidateId = cm.id     
where ca.IRFId = @IRFId and ca.ClientHRAttemptContact = @emailId) Cnd      
left join (select im.id as imid,im.interviewDate, im.confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,    
ic.VideoLink, im.candidateEmail, im.IRFId from InterviewMeetings im join    
(select candidateEmail, max(interviewDate) as intereviewDate from InterviewMeetings where IRFId = @IRFName group by candidateEmail) Filtered    
on im.candidateEmail= Filtered.candidateEmail and im.interviewDate=Filtered.intereviewDate    
left join InterviewComments ic on ic.InterviewId = im.id) ims on ims.candidateEmail= Cnd.EmailId and Cnd.IRFName = ims.IRFId    
end

else
begin
select Cnd.*, ims.confirmationStatus, ims.interviewDate, ims.MeetingStatus, ims.InterviewStatus, ims.VideoLink, ims.imid from     
(select ca.id, ca.IRFId, ca.ResumeLink,ca.TotalInvites, ca.[Type], ca.[Location], ca.Skill, ca.WorkAuth, ca.CurrentLocation, ca.CurrentEmployer,    
ca.TotalExperience, ca.CurrentSalary, ca.ExpectedSalary, ca.Notice, ca.SelectabilityComments,ca.Heuristicsscore,    
ca.JoinabilityComments, ca.JoinabilityScore, ca.SelectabilityScore, ca.TechSkillComments, ca.SkillGeneralComments,ca.CostEstimate,    
ca.TechSkillScore, ca.SkillGeneralScore, ca.AddedByUserId, ca.VoiceItUserId, ca.InviteDate, ca.[Status], ul.EmailId, ca.Phone,    
case 
	 when ul.LastName is null then ul.FirstName
	 else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
end as [Name],
IRF.EndDate, IRF.IRFId as IRFName,cm.id as CandidateMasterId ,Isnull(ca.EncodedResumeLink,'') as EncodedResumeLink, ca.AvailableFullTime, ca.AvailableFreeLancing,    
tasc.AttemptsCount, ca.ClientHRAttemptContact, ca.ProfileSource,ul.WhatsappNumber,ca.IsProctor    
from Candidate ca join IRF on ca.IRFId=IRF.id    
join CandidatesMaster cm on ca.CandidateMasterId=cm.id      
join UserList ul on ul.id = cm.UserId    
left join @tblAttemptStatusCount tasc on tasc.CandidateId = cm.id     
where ca.IRFId = @IRFId) Cnd      
left join (select im.id as imid,im.interviewDate, im.confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,    
ic.VideoLink, im.candidateEmail, im.IRFId from InterviewMeetings im join    
(select candidateEmail, max(interviewDate) as intereviewDate from InterviewMeetings where IRFId = @IRFName group by candidateEmail) Filtered    
on im.candidateEmail= Filtered.candidateEmail and im.interviewDate=Filtered.intereviewDate    
left join InterviewComments ic on ic.InterviewId = im.id) ims on ims.candidateEmail= Cnd.EmailId and Cnd.IRFName = ims.IRFId    

end
    
End 
GO


 

