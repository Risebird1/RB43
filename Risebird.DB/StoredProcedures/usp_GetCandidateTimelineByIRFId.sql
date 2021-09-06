 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateTimelineByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateTimelineByIRFId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
    
    
CREATE PROCEDURE [dbo].[usp_GetCandidateTimelineByIRFId] 
@irfid int  
AS      
Begin 

declare  @addedStatusId int    
    
 set @addedStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Added')    
    
declare @CandidateStatusGrouped table (IRFId int, CandidateId int)    
    insert into @CandidateStatusGrouped     
    select IRFId, CandidateId from CandidateStatus cs where [Status]=@addedStatusId and IRFId= @irfid   
    
    
declare  @meetingStatusId int, @interviewStatusId int, @qcPendingId int;    
    
 set @meetingStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Meeting Status')    
 set @interviewStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Interview Status')    
 set @qcPendingId = (select id from CandidateStatusOptionsMst where StatusValue='QC Pending')--19   
   
 declare @allStatustable table (CandidateMasterId int, IRFId int, [Date] datetime, StatusValue varchar(50), Comments varchar(max))    
    insert into @allStatustable     
    select cs.CandidateId, cs.IRFId, cs.[Date], csom.StatusValue, cs.Comments from CandidateStatus cs   
 join CandidateStatusOptionsMst csom  on cs.[Status] = csom.id   
 join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId)  
  
    
    
 declare @meetingStatusTable table (CandidateMasterId int, IRFId int, [Date] datetime, Comments varchar(max))    
    insert into @meetingStatusTable     
    select cs.CandidateId, cs.IRFId, [Date], Comments from CandidateStatus cs join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId) where [Status]=@meetingStatusId     
    
 declare @interviewStatusTable table (CandidateMasterId int, IRFId int, [Date] datetime, Comments varchar(max))    
    insert into @interviewStatusTable     
    select cs.CandidateId, cs.IRFId, [Date], Comments from CandidateStatus cs join @CandidateStatusGrouped csg on (csg.CandidateId=cs.CandidateId and csg.IRFId=cs.IRFId)   
 where ([Status]=@interviewStatusId  or [Status]= @qcPendingId)  
        
    select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId,    
  case     
   when ul.LastName is null then ul.FirstName    
   else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))    
  end as [Name],    
  ca.ClientHRAttemptContact as HREmail, ca.IRFId, ca.ProfileSource, im.id as InterviewId,    
  ast.[Date] as ScheduleStatusDate, ast.Comments as ScheduleStatusComments, ast.[StatusValue] as ScheduleStatusValue,    
  mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,    
  ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,    
  ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus, irf.IRFId as IRFName, ca.Phone,  im.interviewDate,  
  (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle) -1) ELSE JDTiltle END) as PrimarySkill    
  from Candidate ca     
  join CandidatesMaster cm on cm.id =ca.CandidateMasterId    
  join UserList ul on ul.id =cm.UserId    
  join IRF on IRF.id=ca.IRFId    
  left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId    
  left join InterviewComments ic on ic.InterviewId = im.id    
  left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id    
  join @allStatustable ast on ca.CandidateMasterId = ast.CandidateMasterId and ca.IRFId=ast.IRFId     
  left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId    
  left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId    
  Where IRF.id=@irfid
     
        
    
End 
GO