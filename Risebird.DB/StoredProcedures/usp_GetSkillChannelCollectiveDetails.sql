IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSkillChannelCollectiveDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelCollectiveDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_GetSkillChannelCollectiveDetails                          
@irfId int,                          
@type varchar(20),                        
@timeNow datetime = null                        
AS                          
BEGIN                          
if(@type='Completion')                          
begin                          
Declare @NeedClarity int, @ProbableNoShowCount int, @NotProcessedCandidateResume int ,@MissedCandidate int,@LatejoinCount int,@UnassignedCount int,                
@NoShowInterviewer int, @ColdCandidate int, @BouncedOrDroppedCount int             
--Need clarity                        
Select @NeedClarity=Count(im.id) From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId                         
where CandidateResponse=4 and confirmationStatus=1 and IRF.id=@irfId                        
                        
                        
set  @ProbableNoShowCount =(select COUNT(pnc.id) from ProbableNoShowCandidates pnc                              
join InterviewMeetings im on im.id = pnc.InterviewId                           
join UserList ul on ul.id = pnc.CandidateUserId                              
join InterviewComments ic on ic.Interviewid=im.id                          
inner join IRF on im.IRFId=irf.IRFId                           
left join UserList uli on uli.id=  irf.RisebirdHr where irf.id =@irfId and im.interviewDate >= @timeNow)                           
                        
set @NotProcessedCandidateResume = (Select count(*) from CandidateResume where IsnUll(Isprocessed,0)=0 and IRFId = @irfId)                           
                    
--Missed Candidate                      
Select @MissedCandidate=Count(Distinct im.id)                      
from InterviewMeetings im                                  
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId                          
Left join InterviewComments ic on ic.InterviewId=im.id                                
left join TransZoomMeetingExtendDetails tzme on im.ZoomMeetingId = tzme.Meetingid                            
outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc                          
where IRF.id=@irfId and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission)                         
and ((im.IsRescheduleNotificationSent=1 and ic.MeetingStatus!=3 and ic.MeetingStatus!=4 and ic.MeetingStatus!=6 and ic.MeetingStatus!=7 and ic.MeetingStatus!=8)                        
or ISNULL(imc.PerformedOn,'')!='' and(ic.MeetingStatus=1 or ic.MeetingStatus=5 or ic.MeetingStatus=9))                        
                    
--Late Join                    
;with CTE as (Select im.interviewer,Count(distinct im.id) as TotalLatejoin                    
from IRF ir join InterviewMeetings im on im.IRFId=ir.IRFId                    
join InterviewMeetingsJoinTime imj on im.id=imj.interviewid join InterviewComments ic on ic.Interviewid=im.id                         
where DATEDIFF(MINUTE,interviewDate,Joinedon)>5 and (ic.MeetingStatus=1 or ic.MeetingStatus=5 or ic.MeetingStatus=9) and ir.id=@irfId                    
group by im.IRFId,im.interviewer)                    
select @LatejoinCount=Count(CTE.interviewer) from CTE                    
                  
--Unassigned Interview                  
Select @UnassignedCount=Count(distinct im.id) from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where confirmationStatus=1 and interviewer is null and IRF.id=@irfId                  
                
--No Show Interviewer                
Select @NoShowInterviewer=Count(distinct im.id) from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId                 
join InterviewComments ic on ic.InterviewId=im.id                
where confirmationStatus=1 and IRF.id=@irfId  and (ic.MeetingStatus=5 or ic.MeetingStatus=9)                
              
--Cold Candidate              
Select @ColdCandidate=Count(distinct ul.EmailId) from IRF join Candidate ca on ca.IRFId=IRF.id            
join ColdCandidateInfos cci on cci.FKCandidateId=ca.id            
join CandidatesMaster cm on cm.id=ca.CandidateMasterId join UserList ul on ul.id=cm.UserId            
where IRF.id=@irfId and ISNULL(cci.IsColdCandidate,0)=1              
          
--Bounced or Dropped          
set @BouncedOrDroppedCount =           
(          
  select count (distinct sge.email)           
  from SentMailDetails smd          
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id          
  join UserList ul on ul.EmailId = sge.email          
  where sge.email != 'hire@risebird.io'          
  and ([event] = 'bounce' or [event] = 'dropped')          
  and smd.IRFId = @irfId          
  and ul.idProfile=23           
)          
              
Select @ProbableNoShowCount as ProbableNoShowCount, @NotProcessedCandidateResume as NotProcessedResumeCount,           
@NeedClarity as NeedClarity,@MissedCandidate as MissedCandidate,@LatejoinCount as LatejoinCount,@UnassignedCount as UnassignedCount,                
@NoShowInterviewer as NoShowInterviewer,@ColdCandidate as ColdCandidate,  @BouncedOrDroppedCount as BouncedOrDroppedCount,              
(select top 1                        
CASE                        
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'ProbableNoshow' and irfid = @irfId order by CreatedOn desc) as ProbableNoShowLatestStatus,                        
(select top 1                        
CASE                        
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'NotProcessedResumes' and irfid = @irfId order by CreatedOn desc) as NotProcessedResumesLatestStatus,                        
(select top 1                        
CASE                        
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'MissedCandidates' and irfid = @irfId order by CreatedOn desc) as MissedCandidatesLatestStatus,                    
(select top 1                        
CASE                        
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'Latejoin' and irfid = @irfId order by CreatedOn desc) as LatejoinStatus,                    
(select top 1                        
CASE                        
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'Unassigned' and irfid = @irfId order by CreatedOn desc) as UnassignedStatus,                  
(select top 1                        
CASE                        
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'NeedClarity' and irfid = @irfId order by CreatedOn desc) as NeedClarityStatus,                
(select top 1                        
CASE                        
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'NoShowInterviewer' and irfid = @irfId order by CreatedOn desc) as NoShowInterviewerStatus,              
(select top 1               
CASE                        
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'ColdCandidate' and irfid = @irfId order by CreatedOn desc) as ColdCandidateStatus,          
(select top 1                    
CASE                    
    WHEN [Status] = 1 THEN 'In Progress'                    
    WHEN [Status] = 2 THEN 'Completed'                  
    ELSE 'In Progress'                    
END as StatusValue                    
from SkillChannelTasks where TaskValue = 'BouncedOrDropped' and irfid = @irfId order by CreatedOn desc) as BouncedOrDroppedTaskLatestStatus          
                        
                        
end            
else if(@type='accuracy')                        
begin                          
                          
Declare @HighSelectCount int                          
Declare @HighRejectCount int                          
Declare @HighNegCandidateRating int,@HighNegClientRating int,@RedoRequests int,@LessSelectComments int,@LessRejectComments int,@LessQuestions int                          
--High Selection Rates                          
declare @HighSelect as Table                          
(                          
interviewer varchar(150),                          
selectionrate int                          
)                          
;With CTE as(                          
select im.interviewer, count(distinct ic.id) totalCompleted,count(distinct ics.id) selectedMeetings from IRF ir                          
join InterviewMeetings im on im.IRFId=ir.IRFId                          
left join InterviewComments ic on ic.InterviewId = im.id  and (ic.MeetingStatus in (4, 6, 7, 8))                          
left join InterviewComments ics on ics.InterviewId = im.id and (ics.InterviewStatus = 2 or ics.InterviewStatus = 4)                          
where  ir.id= @irfId                           
group by im.interviewer)                          
insert into @HighSelect                          
Select interviewer,case when totalCompleted>0 then(ISNULL(selectedMeetings,0)*100/totalCompleted) else 0 end selectionrate From CTE                           
where case when totalCompleted>0 then(ISNULL(selectedMeetings,0)*100/totalCompleted) else 0 end>=40                          
                          
--High Rejection Rate                          
declare @HighReject as Table                          
(                          
interviewer varchar(150),                          
rejectionrate int                          
)                          
;With CTE as(                          
select im.interviewer, count(distinct ic.id) totalCompleted,count(distinct ics.id) rejectmeetings from IRF ir                          
join InterviewMeetings im on im.IRFId=ir.IRFId                          
left join InterviewComments ic on ic.InterviewId = im.id  and (ic.MeetingStatus in (4, 6, 7, 8))                          
left join InterviewComments ics on ics.InterviewId = im.id and (ics.InterviewStatus = 3 or ics.InterviewStatus = 5)                           
where  ir.id= @irfId                           
group by im.interviewer)                          
insert into @HighReject                          
Select interviewer,case when totalCompleted>0 then(ISNULL(rejectmeetings,0)*100/totalCompleted) else 0 end rejectionrate From CTE                           
where case when totalCompleted>0 then(ISNULL(rejectmeetings,0)*100/totalCompleted) else 0 end>=40                          
                          
--Potential risk by candidate                           
;With CTE as(select im.interviewer,count(distinct im.id) totalnegrating from IRF ir                          
join InterviewMeetings im on im.IRFId=ir.IRFId                          
join CandidateFeedbackDetails cd on cd.InterviewId=im.id                          
where cd.Rating<4 and ir.id=@irfId                          
group by im.interviewer)                          
Select @HighNegCandidateRating=Count(*) From CTE                         
        
--Potential risk by Client         
select @HighNegClientRating=Count(distinct ccfr.id)         
from  ClientCommentsForReport ccfr         
join InterviewMeetings im on ccfr.InterviewId=im.id        
join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id         
where ccfr.Rating<=3 and IRF.id=@irfId        
      
--Redo Request      
Select @RedoRequests=Count(distinct ri.id)
from RedoInterviews ri join InterviewMeetings im on ri.interviewid=im.id  
inner join IRF on im.IRFId=irf.IRFId  
left join UserList ulc on im.candidateEmail= ulc.EmailId   
left join UserList ulco on ulco.id=irf.RisebirdHr
where ISNULL(Approved,0)=1 and IRF.id=@irfId
    
--Select Less Comment    
;with CTE as (Select im.interviewer,Count(distinct im.id) as TotallessComment    
from IRF ir join Interviewmeetings im on im.IRFId=ir.IRFId    
join InterviewComments ic on im.id=ic.InterviewId     
where confirmationStatus=1 and ir.id=@irfId    
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)     
and (InterviewStatus=2 or InterviewStatus=4)    
and Len(ic.InterviewerComment)<500     
group by im.interviewer)    
select @LessSelectComments=Count(distinct interviewer) from CTE    
    
--Reject Less Comment    
;with CTE as (Select im.interviewer,Count(distinct im.id) as TotallessComment    
from IRF ir join Interviewmeetings im on im.IRFId=ir.IRFId    
join InterviewComments ic on im.id=ic.InterviewId     
where confirmationStatus=1 and ir.id=@irfId    
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)     
and (InterviewStatus=3 or InterviewStatus=5)    
and Len(ic.InterviewerComment)<250     
group by im.interviewer)    
select @LessRejectComments=Count(distinct interviewer) from CTE    
   
--Less Questions Interviews  
;with CTE as (Select im.interviewer,Count(distinct im.id) as TotallessQuestions  
from IRF ir join InterviewMeetings im on im.IRFId=ir.IRFId join QuestionsDiscussed qd on im.id=qd.InterviewId    
join InterviewComments ic on im.id=ic.InterviewId     
where confirmationStatus=1   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)  
group by im.interviewer  having Count(qd.id)<5)  
select @LessQuestions=Count(distinct interviewer) from CTE  
  
--Return the result count                          
Select @HighSelectCount= Count(*) from @HighSelect                          
Select @HighRejectCount= Count(*) From @HighReject                          
                          
Select @HighSelectCount as HighSelectCount,@HighRejectCount as HighRejectCount,@HighNegCandidateRating as HighNegCandidateRating,@HighNegClientRating as HighNegClientRating,      
@RedoRequests as RedoRequests,@LessSelectComments as LessSelectComments,@LessRejectComments as LessRejectComments,@LessQuestions as LessQuestions,      
(select top 1                        
CASE                        
    WHEN [Status] = 1 THEN 'In Progress'                
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'HighSelectionRate' and irfid = @irfId order by CreatedOn desc) as HighSelectionRateLatestStatus,                        
(select top 1                        
CASE                        
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'HighRejectionRate' and irfid = @irfId order by CreatedOn desc) as HighRejectionRateLatestStatus,                        
(select top 1                        
CASE                   
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'PotentialRiskByCandidate' and irfid = @irfId order by CreatedOn desc) as PotentialRiskByCandidateLatestStatus,                          
(select top 1                        
CASE                   
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'PotentialRiskByClient' and irfid = @irfId order by CreatedOn desc) as PotentialRiskByClientLatestStatus,                                          
(select top 1                        
CASE                   
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'      
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'RedoRequest' and irfid = @irfId order by CreatedOn desc) as RedoRequestStatus,    
(select top 1                        
CASE                   
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'LessSelectComment' and irfid = @irfId order by CreatedOn desc) as LessSelectCommentStatus,    
(select top 1                        
CASE                   
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'LessRejectComment' and irfid = @irfId order by CreatedOn desc) as LessRejectCommentStatus,    
(select top 1                        
CASE                   
    WHEN [Status] = 1 THEN 'In Progress'                        
    WHEN [Status] = 2 THEN 'Completed'                        
    ELSE 'In Progress'                        
END as StatusValue                        
from SkillChannelTasks where TaskValue = 'LessQuestions' and irfid = @irfId order by CreatedOn desc) as LessQuestionsStatus    
end                          
else if(@type='speed')                          
begin                          
Declare @CalibrationCall bit=0                        
Declare @DMApproved bit=0                        
Declare @CalibrationCallCount int                        
Declare @ReportPending int                        
--Calibration Call                        
Select @CalibrationCallCount =count(id) From CalibrationMeetings where irfid=@irfId and MeetingDate<=DATEADD(Minute,330,Getdate())                         
if(@CalibrationCallCount>0)                        
begin      set @CalibrationCall=1                        
end                        
--DM Approval                        
Select @DMApproved=ISNULL(DMApproved,0) from IRF where id=@irfId                        
                        
--Report Pending                        
select @ReportPending= Count(distinct im.id) from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id                         
left join InterviewComments ic on ic.InterviewId= im.id                         
left join UserList uli on uli.EmailId=im.interviewer                        
left join UserList ulc on ulc.id=IRF.RisebirdHr                        
where IRF.id=@irfId and  confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewdate<DATEADD(Minute,150,Getdate())                         
                        
--Return the result                        
Select @CalibrationCall as CalibrationCall,@DMApproved as DMApproved,@ReportPending as ReportPending                        
end                        
else if(@type='security')                          
begin                          
Declare @ProxyCount int                        
--Interviewer Marked Candidate as proxy                        
Select @ProxyCount=Count(im.id) From InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId                         
join IRF on IRF.IRFId=im.IRFId                        
where ic.InterviewStatus=1 and IRF.id=@irfId and confirmationStatus=1                        
                        
--Return the result                        
Select @ProxyCount as ProxyCount                       
end                          
END 
GO