
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateLatestStatusForApi'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateLatestStatusForApi
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetCandidateLatestStatusForApi]    
@skillChannelName varchar(25) = null,     
@candidateEmailId varchar(150) ,
@requestedByClientUser varchar(200),
@jobId varchar(20) = null,
@timeNow datetime
AS    
Begin  
Declare @RoleId int = (select RoleId From ClientUser cu join UserList ul on cu.UserId=ul.id where ul.EmailId=@requestedByClientUser)  
Declare @ClientUserId int = (select id From  UserList where EmailId=@requestedByClientUser) 

declare @IRFId int  = (select id from IRF where IRFId = @skillChannelName)
declare @candidateMasterId int  = (select id from CandidatesMaster where userId = (select id from UserList where EmailId = @candidateEmailId))

DECLARE @TemporaryCandidateTableVariable TABLE (                    
 IRFId int,              
 CandidateMasterId int,              
 UserId int,         
 CandidateLatestStatus varchar(20),      
 InterviewId int null,      
 interviewDate datetime null,
 addedOn datetime null,
 statusUpdatedAt datetime null  
);     
  
DECLARE @TemporaryCandidateTableFinal TABLE (                    
 IRFId int,              
 CandidateMasterId int,              
 UserId int,         
 CandidateLatestStatus varchar(20),      
 InterviewId int null,      
 interviewDate datetime null,
 addedOn datetime null,
 statusUpdatedAt datetime null
); 


if(@IRFId is not null and @IRFId != 0)
Begin
	INSERT INTO @TemporaryCandidateTableVariable      
	Select distinct ca.IRFId,ca.CandidateMasterId,cm.UserId,la.LatestStatus,la.interviewId,la.interviewDate, la.addedOn, la.statusUpdatedAt   
	from Candidate ca 
	join CandidatesMaster cm on  ca.CandidateMasterId = cm.id
	outer apply dbo.fn_CandidateLatestStatusForApi(cm.UserId, ca.IRFId, @timeNow) La      
	where ca.IRFId = @irfId and ca.CandidateMasterId = @candidateMasterId
End
Else if(@jobId is not null and @jobId != '')
Begin
	INSERT INTO @TemporaryCandidateTableVariable      
	Select distinct ca.IRFId,ca.CandidateMasterId,cm.UserId,la.LatestStatus,la.interviewId,la.interviewDate, la.addedOn, la.statusUpdatedAt   
	from Candidate ca 
	join CandidatesMaster cm on  ca.CandidateMasterId = cm.id
	join IRF on irf.id = ca.IRFId
	join ProjectIRFMapping pim on IRF.id=pim.IRFId join Project po on pim.ProjectId=po.id
	outer apply dbo.fn_CandidateLatestStatusForApi(cm.UserId, ca.IRFId, @timeNow) La      
	where po.ProjectUniqueid=@jobId and ca.CandidateMasterId = @candidateMasterId--ca.IRFId = @irfId 
End

if(@Roleid=1)  
begin  
Insert into @TemporaryCandidateTableFinal  
Select td.* from @TemporaryCandidateTableVariable td join Candidate ca on ca.CandidateMasterId=td.CandidateMasterId and ca.IRFId=td.IRFId  
join TransClientUser tc on tc.IRFId=ca.IRFId

where ca.ClientHRAttemptContact=@requestedByClientUser and tc.ClientUserId= @ClientUserId
end  
else if(@Roleid=4)  
begin  
Insert into @TemporaryCandidateTableFinal  
Select td.* from @TemporaryCandidateTableVariable td join Candidate ca on ca.CandidateMasterId=td.CandidateMasterId and ca.IRFId=td.IRFId  
join TransClientUser tc on tc.IRFId=ca.IRFId    
left join InterviewMeetings im on im.id=td.InterviewId  
where (ca.ProfileSource=@requestedByClientUser or im.staffingAgency=@requestedByClientUser) and tc.ClientUserId= @ClientUserId
end  
else if(@Roleid=2 or @Roleid=6)  
begin   
Insert into @TemporaryCandidateTableFinal  
Select td.* from @TemporaryCandidateTableVariable td   
join TransClientUser tc on tc.IRFId=td.IRFId
where tc.ClientUserId= @ClientUserId 
end  
else  
begin  
Insert into @TemporaryCandidateTableFinal  
Select td.* from @TemporaryCandidateTableVariable td  
end 

select im.id, im.IRFId,IRF.id as irfintid,im.interviewDate, im.candidateEmail,IRF.JDTiltle,IRF.JDTitleTwo,ic.MeetingStatus,      
ic.InterviewStatus, ic.InterviewerComment, ic.VideoLink, im.confirmationStatus, ic.CommunicationalProficiency, 
ice.MeetingStatus as MeetingStatusTwo,ice.InterviewStatus as InterviewStatusTwo,
ice.InterviewerComment as InterviewerCommentTwo, Project.ProjectUniqueid as jobId, 
CASE
    WHEN CandidateLatestStatus like '%FUTURESCHEDULE%' THEN 'SCHEDULED'
    ELSE CandidateLatestStatus
END AS CandidateLatestStatus,
ipr.NickName as InterviewerCodedName ,PrimarySkill.TechScore,SecondarySkill.TechScoreTwo,IRF.SkillSetsevaluate,
addedOn, statusUpdatedAt
--from @TemporaryCandidateTableVariable tctv   
from @TemporaryCandidateTableFinal tctv
join InterviewMeetings im on im.id = tctv.InterviewId      
join IRF on IRF.IRFId=im.IRFId       
join InterviewComments ic on ic.InterviewId=im.id      
left join InterviewCommentsExtension ice on ice.Interviewid = im.id      
join UserList ul on ul.EmailId = im.candidateEmail      
left join UserList uli on uli.EmailId = im.interviewer      
left join InterviewerProfile ipr on ipr.UserId = uli.id      
outer apply(select Sum(WeightageActual) as TechScore From TechSkillReport ts where ts.InterviewId=im.id and (ts.SkillSet = 1 or ts.SkillSet is null)) PrimarySkill  
outer apply(select Sum(WeightageActual) as TechScoreTwo From TechSkillReport ts where ts.InterviewId=im.id and ts.SkillSet = 2) SecondarySkill 
outer apply (Select top 1 ProjectUniqueid From ProjectIRFMapping pim join Project pr on pr.id=pim.ProjectId where pim.IRFId=IRF.id) Project 
where CandidateLatestStatus like '%SELECTED%' or CandidateLatestStatus like '%REJECTED%' 
or CandidateLatestStatus like '%PENDING%' or CandidateLatestStatus like '%FUTURESCHEDULE%'         
or CandidateLatestStatus like '%NOTSCHEDULED%' or CandidateLatestStatus like '%COLDCANDIDATE%'
or CandidateLatestStatus like '%SHOW%' or CandidateLatestStatus like '%CANCEL%'   
or CandidateLatestStatus like '%DROP%'
  
Select qd.InterviewId,qd.Comment,qd.SkillName,qd.SkillSet,qd.RatingId From QuestionsDiscussed qd 
join @TemporaryCandidateTableVariable td on qd.InterviewId=td.InterviewId  

Select tsr.Interviewid,tsr.SkillName,tsr.WeightageRequired,tsr.WeightageActual,tsr.SkillSet From TechSkillReport tsr 
join @TemporaryCandidateTableVariable td on tsr.InterviewId=td.InterviewId                 
End  
GO


