
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateLatestStatusForApi'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateLatestStatusForApi
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetCandidateLatestStatusForApi]    
@skillChannelName varchar(25),     
@candidateEmailId varchar(150) ,
@requestedByClientUser varchar(200),
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

INSERT INTO @TemporaryCandidateTableVariable      
Select distinct ca.IRFId,ca.CandidateMasterId,cm.UserId,la.LatestStatus,la.interviewId,la.interviewDate, la.addedOn, la.statusUpdatedAt   
from Candidate ca
--InterviewMeetings im
--join IRF on IRF.IRFId=ca.IRFId  
--join UserList ul on ul.EmailId=im.candidateEmail  
join CandidatesMaster cm on  ca.CandidateMasterId = cm.id-- cm.UserId=ul.id  
outer apply dbo.fn_CandidateLatestStatusForApi(cm.UserId, ca.IRFId, @timeNow) La      
where ca.IRFId = @irfId and ca.CandidateMasterId = @candidateMasterId

if(@Roleid=1)  
begin  
Insert into @TemporaryCandidateTableFinal  
Select td.* from @TemporaryCandidateTableVariable td join Candidate ca on ca.CandidateMasterId=td.CandidateMasterId and ca.IRFId=td.IRFId  
join TransClientUser tc on tc.IRFId=ca.IRFId --and tc.ClientUserId=td.UserId  
--join UserList ul on ul.id=td.UserId  
where ca.ClientHRAttemptContact=@requestedByClientUser and tc.ClientUserId= @ClientUserId --ul.EmailId=@requestedByClientUser  
end  
else if(@Roleid=4)  
begin  
Insert into @TemporaryCandidateTableFinal  
Select td.* from @TemporaryCandidateTableVariable td join Candidate ca on ca.CandidateMasterId=td.CandidateMasterId and ca.IRFId=td.IRFId  
join TransClientUser tc on tc.IRFId=ca.IRFId --and tc.ClientUserId=td.UserId  
--join UserList ul on ul.id=td.UserId  
left join InterviewMeetings im on im.id=td.InterviewId  
where (ca.ProfileSource=@requestedByClientUser or im.staffingAgency=@requestedByClientUser) and tc.ClientUserId= @ClientUserId--ul.EmailId=@requestedByClientUser  
end  
else if(@Roleid=2 or @Roleid=6)  
begin   
Insert into @TemporaryCandidateTableFinal  
Select td.* from @TemporaryCandidateTableVariable td   
join TransClientUser tc on tc.IRFId=td.IRFId --and tc.ClientUserId=td.UserId  
--join UserList ul on ul.id=td.UserId  
where tc.ClientUserId= @ClientUserId--ul.EmailId = @requestedByClientUser  
end  
else  
begin  
Insert into @TemporaryCandidateTableFinal  
Select td.* from @TemporaryCandidateTableVariable td  
end 

select im.id, im.IRFId,IRF.id as irfintid,im.interviewDate, im.candidateEmail,IRF.JDTiltle,IRF.JDTitleTwo,ic.MeetingStatus,      
ic.InterviewStatus, ic.InterviewerComment, ic.VideoLink, im.confirmationStatus, ic.CommunicationalProficiency, 
ice.MeetingStatus as MeetingStatusTwo,ice.InterviewStatus as InterviewStatusTwo,
ice.InterviewerComment as InterviewerCommentTwo,
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
where CandidateLatestStatus like '%SELECTED%' or CandidateLatestStatus like '%REJECTED%' 
or CandidateLatestStatus like '%PENDING%' or CandidateLatestStatus like '%FUTURESCHEDULE%'         
or CandidateLatestStatus like '%NOTSCHEDULED%' or CandidateLatestStatus like '%COLDCANDIDATE%'
or CandidateLatestStatus like '%SHOW%' or CandidateLatestStatus like '%CANCEL%'   
or CandidateLatestStatus like '%DROP%'
  
Select qd.InterviewId,qd.Comment,qd.SkillName,qd.SkillSet,qd.RatingId From QuestionsDiscussed qd join @TemporaryCandidateTableVariable td on qd.InterviewId=td.InterviewId  
  
 --select top 1 cs.IRFId, cs.CandidateId, cs.AudioFileLink, cs.Comments,
 --DATEADD(minute,330,cs.Date) as Date, cs.Status, cs.PerformedBy, cs.InterviewId,
 --csom.StatusValue, ic.MeetingStatus, im.confirmationStatus, im.interviewDate,
 --tqr.IsReportBlocked, im.RedoInterview  
 --from CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id 
 --left join InterviewMeetings im on im.id=cs.InterviewId 
 --left join InterviewComments ic on ic.InterviewId=cs.InterviewId
 --left join Trans_QCFormReport tqr on tqr.interviewid=im.id
 --where cs.IRFId=@IRFId and CandidateId = @candidateMasterId 
 --order by cs.[Date] desc
  
End  
GO


