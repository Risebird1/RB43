/****** Object:  StoredProcedure [dbo].[usp_GetCandidateTimeline]    Script Date: 11-12-2020 05.54.02 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateStatusDataWithCold'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateStatusDataWithCold
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetCandidateStatusDataWithCold]     
@clientId int,                
@startDate datetime,                
@endDate datetime                
AS                  
Begin           
                 
    
DECLARE @TemporaryCandidateTableVariable TABLE (                    
 IRFId int,              
 CandidateMasterId int,              
 UserId int,        
 CandidateLatestStatus varchar(20),      
 InterviewId int null,      
 interviewDate datetime null,      
 ResumeLink varchar(500) NULL,      
 AddedOnDate datetime null,      
 InvitationSentOnDate datetime null,      
 FirstScheduledOnDate datetime null,      
 FirstScheduledOninterviewDate datetime null,      
 completedDate datetime null,    
 PreReportLink varchar(500) NULL,    
 PreReportUsername varchar(50) NULL,    
 PreReportPassword varchar(50) NULL,  
 Requestor varchar(150) NULL  
);                
     
INSERT INTO @TemporaryCandidateTableVariable                    
select ca.IRFId, ca.CandidateMasterId, cm.UserId, La.LatestStatus, La.interviewId, La.interviewDate,  ca.ResumeLink      
,Journey.AddedOnDate, Journey.InvitationSentOnDate, Journey.FirstScheduledOnDate, Journey.FirstScheduledOninterviewDate,    
La.completedDate, ca.PreReportLink, ca.PreReportUsername, ca.PreReportPassword,ca.ClientHRAttemptContact as Requestor                            
from  Candidate ca              
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)              
join CandidatesMaster cm on cm.id=ca.CandidateMasterId    
join IRF irf on ca.IRFId=irf.id    
outer apply dbo.fn_CandidateLatestStatusForIrfStatsForExcel(cm.UserId, ca.IRFId, GETDATE()) La      
outer apply dbo.fn_CandidateJourneyImportantMileStoneData(cm.UserId, ca.IRFId) Journey      
where cs.[Status] = 12 and irf.ClientId=177 and  [Date] between @StartDate and @EndDate    
group by ca.IRFId, ca.CandidateMasterId,cm.UserId, La.LatestStatus, La.interviewId, La.interviewDate, ca.ResumeLink      
, Journey.AddedOnDate, Journey.InvitationSentOnDate, Journey.FirstScheduledOnDate, Journey.FirstScheduledOninterviewDate,    
La.completedDate, ca.PreReportLink, ca.PreReportUsername, ca.PreReportPassword,ca.ClientHRAttemptContact     
    
select IRF.IRFId,ul.EmailId as candidateEmail, concat(ul.FirstName, ' ', ul.LastName) as candidateName,ul.MobileNumber,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,ca.ClientHRAttemptContact as ClientHrContact,tctv.ResumeLink,csc.StatusValue as CurrentStatus,    
CandidateLatestStatus,tctv.AddedOnDate,tctv.FirstScheduledOnDate,'' as ExecutiveReportLink,csc.Date as CurrentStatusDate,InterviewCount.NoOfInterviews,im.interviewDate,tctv.Requestor,IRF.Heuristicsscore    
from @TemporaryCandidateTableVariable tctv      
join IRF on IRF.id =tctv.IRFId      
join UserList ul on ul.id = tctv.UserId      
join Candidate ca on ca.IRFId=tctv.IRFId and ca.CandidateMasterId=tctv.CandidateMasterId    
outer apply(select max(interviewDate) as interviewDate from InterviewMeetings where IRFId=IRF.IRFId and candidateEmail=ul.EmailId and ISNULL(IsDeleted,0)=0) im    
outer Apply (Select top 1 cso.StatusValue,DateAdd(Minute,330,cs.Date) as Date From CandidateStatus cs join CandidateStatusOptionsMst cso on cso.id=cs.Status where cs.IRFId=ca.IRFId and cs.CandidateId=ca.CandidateMasterId order by cs.Date desc) csc    
outer apply(select Count(id) as NoOfInterviews from InterviewMeetings where IRFId=IRF.IRFId and candidateEmail=ul.EmailId) InterviewCount    
where IRF.ClientId = @clientId and CandidateLatestStatus in ('NOTSCHEDULED','COLDCANDIDATE')    
                  
End             
GO


