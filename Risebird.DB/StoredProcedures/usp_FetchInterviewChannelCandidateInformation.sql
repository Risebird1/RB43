IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchInterviewChannelCandidateInformation'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewChannelCandidateInformation
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchInterviewChannelCandidateInformation 
@irfId int =0,       
@clientid int,      
@ClientUser varchar(200),      
@timeNow datetime,           
@startdate datetime=null,      
@enddate datetime =null     
AS
BEGIN
Declare @RoleId int      
Select @RoleId=RoleId From ClientUser cu join UserList ul on cu.UserId=ul.id where ul.EmailId=@ClientUser      
      
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
----Candidate Added Date      
--INSERT INTO @TemporaryCandidateTableVariable                        
--select ca.IRFId, ca.CandidateMasterId, cm.UserId, La.LatestStatus, La.interviewId, La.interviewDate                 
--from  Candidate ca                   
--join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)                   
--join CandidatesMaster cm on cm.id=ca.CandidateMasterId            
--outer apply dbo.fn_CandidateLatestStatusForIrfStatsForExcel(cm.UserId, ca.IRFId, @timeNow) La          
--where cs.[Status] = 12 and  ca.IRFId = @irfId-- cast([Date] as date) <=cast(@endDate as date) and cast([Date] as date) >= cast(@startDate as date)                  
--group by ca.IRFId, ca.CandidateMasterId,cm.UserId, La.LatestStatus, La.interviewId, La.interviewDate, ca.ResumeLink , La.completedDate           
      
----Interview Date      
      
if(@startdate is not null and @enddate is not null)     
begin      
if(@irfId!=0)       
begin      
INSERT INTO @TemporaryCandidateTableVariable       
Select distinct ca.IRFId,ca.CandidateMasterId,cm.UserId,la.LatestStatus,la.interviewId,la.interviewDate,la.addedOn,la.statusUpdatedAt       
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId      
join UserList ul on ul.EmailId=im.candidateEmail      
join CandidatesMaster cm on cm.UserId=ul.id      
join Candidate ca on ca.IRFId=IRF.id and ca.CandidateMasterId=cm.id      
outer apply dbo.fn_CandidateLatestStatusForIrfStatsForExcel(cm.UserId, ca.IRFId, @timeNow) La          
where IRF.id=@irfId and im.interviewDate>@startdate and im.interviewDate<@enddate      
end      
else      
begin      
INSERT INTO @TemporaryCandidateTableVariable       
Select distinct ca.IRFId,ca.CandidateMasterId,cm.UserId,la.LatestStatus,la.interviewId,la.interviewDate,la.addedOn,la.statusUpdatedAt       
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId      
join UserList ul on ul.EmailId=im.candidateEmail      
join CandidatesMaster cm on cm.UserId=ul.id      
join Candidate ca on ca.IRFId=IRF.id and ca.CandidateMasterId=cm.id      
outer apply dbo.fn_CandidateLatestStatusForIrfStatsForExcel(cm.UserId, ca.IRFId, @timeNow) La          
where IRF.clientid=@clientid and im.interviewDate>@startdate and im.interviewDate<@enddate      
end    
delete From @TemporaryCandidateTableVariable where interviewDate not between @startdate and @enddate      
end      
else if(@irfId!=0)          
begin      
INSERT INTO @TemporaryCandidateTableVariable          
Select distinct ca.IRFId,ca.CandidateMasterId,cm.UserId,la.LatestStatus,la.interviewId,la.interviewDate,la.addedOn,la.statusUpdatedAt       
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId      
join UserList ul on ul.EmailId=im.candidateEmail      
join CandidatesMaster cm on cm.UserId=ul.id      
join Candidate ca on ca.IRFId=IRF.id and ca.CandidateMasterId=cm.id      
outer apply dbo.fn_CandidateLatestStatusForIrfStatsForExcel(cm.UserId, ca.IRFId, @timeNow) La          
where IRF.id=@irfId      
end      
      
if(@Roleid=1)      
begin      
Insert into @TemporaryCandidateTableFinal      
Select td.* from @TemporaryCandidateTableVariable td join Candidate ca on ca.CandidateMasterId=td.CandidateMasterId and ca.IRFId=td.IRFId      
join TransClientUser tc on tc.IRFId=ca.IRFId and tc.ClientUserId=td.UserId      
join UserList ul on ul.id=td.UserId      
where ca.ClientHRAttemptContact=@ClientUser and ul.EmailId=@ClientUser      
end      
else if(@Roleid=4)      
begin      
Insert into @TemporaryCandidateTableFinal      
Select td.* from @TemporaryCandidateTableVariable td join Candidate ca on ca.CandidateMasterId=td.CandidateMasterId and ca.IRFId=td.IRFId      
join TransClientUser tc on tc.IRFId=ca.IRFId and tc.ClientUserId=td.UserId      
join UserList ul on ul.id=td.UserId      
left join InterviewMeetings im on im.id=td.InterviewId      
where (ca.ProfileSource=@ClientUser or im.staffingAgency=@ClientUser) and ul.EmailId=@ClientUser      
end      
else if(@Roleid=2 or @Roleid=6)      
begin       
Insert into @TemporaryCandidateTableFinal      
Select td.* from @TemporaryCandidateTableVariable td       
join TransClientUser tc on tc.IRFId=td.IRFId and tc.ClientUserId=td.UserId      
join UserList ul on ul.id=td.UserId      
where ul.EmailId=@ClientUser      
end      
else      
begin      
Insert into @TemporaryCandidateTableFinal      
Select td.* from @TemporaryCandidateTableVariable td      
end      
      
----completed          
select im.id, im.IRFId,IRF.id as irfintid,im.interviewDate, im.candidateEmail,IRF.JDTiltle,IRF.JDTitleTwo,ic.MeetingStatus,          
ic.InterviewStatus, ic.InterviewerComment, ic.VideoLink, im.confirmationStatus, ic.CommunicationalProficiency,     
ice.MeetingStatus as MeetingStatusTwo,ice.InterviewStatus as InterviewStatusTwo,    
ice.InterviewerComment as InterviewerCommentTwo,
ipr.NickName as InterviewerCodedName ,PrimarySkill.TechScore,SecondarySkill.TechScoreTwo,IRF.SkillSetsevaluate,tctv.addedOn,tctv.statusUpdatedAt,
Case When CandidateLatestStatus like '%FUTURESCHEDULE%' then 'SCHEDULED' else CandidateLatestStatus end as CandidateLatestStatus
from @TemporaryCandidateTableVariable tctv          
join InterviewMeetings im on im.id = tctv.InterviewId          
join IRF on IRF.IRFId=im.IRFId           
join InterviewComments ic on ic.InterviewId=im.id          
left join InterviewCommentsExtension ice on ice.Interviewid = im.id          
join UserList ul on ul.EmailId = im.candidateEmail          
left join UserList uli on uli.EmailId = im.interviewer          
left join InterviewerProfile ipr on ipr.UserId = uli.id          
outer apply(select Sum(WeightageActual) as TechScore From TechSkillReport ts where ts.InterviewId=im.id and (ts.SkillSet = 1 or ts.SkillSet is null)) PrimarySkill      
outer apply(select Sum(WeightageActual) as TechScoreTwo From TechSkillReport ts where ts.InterviewId=im.id and ts.SkillSet = 2) SecondarySkill      
where CandidateLatestStatus like '%SELECTED%' or CandidateLatestStatus like '%REJECTED%' or CandidateLatestStatus like '%PENDING%' or CandidateLatestStatus like '%FUTURESCHEDULE%'             
      
Select qd.InterviewId,qd.Comment,qd.SkillName,qd.SkillSet,qd.RatingId From QuestionsDiscussed qd join @TemporaryCandidateTableVariable td on qd.InterviewId=td.InterviewId         
END   
  
  
GO