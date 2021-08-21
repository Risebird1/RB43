
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllIRFDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllIRFDetails
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  
    
    
CREATE PROCEDURE [dbo].[usp_GetAllIRFDetails]     
@startDate datetime,
@endDate datetime

AS 
Begin

DECLARE @tbl_Temp TABLE (    
    IRFId INT      
);    
INSERT INTO @tbl_Temp    
select distinct IRFId from CandidateStatus where [Status] =12 and [Date]<=@endDate and [Date] > @startDate 


DECLARE @TemporaryCandidateTableVariable AS tempCandidate_TableType  
INSERT INTO @TemporaryCandidateTableVariable    
select ca.id from  Candidate ca join CandidateStatus cs on (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId) where cs.[Status] =12 and [Date]<=@endDate and [Date] > @startDate 

--DECLARE @tbl_CandidateTemp TABLE (    
--    id INT      
--);    
--INSERT INTO @tbl_CandidateTemp    
--select ca.id from  Candidate ca join CandidateStatus cs on (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId) where cs.[Status] =12 and [Date]<=@endDate and [Date] > @startDate 


	select irf.id, irf.IRFId, irf.JDTiltle, cl.[Name] as ClientCompName, irf.JobCountry, IsNULL(ulb.EmailId, '') as BillingOwnerEmailId, ul.EmailId as RisebirdHrEmail, 
	(select count(ca.id) from Candidate ca join @TemporaryCandidateTableVariable tmpC on tmpC.id =ca.id where IRFId = irf.id) as CandidateCount,
	(select count(im.id) from irf as irfCI left join InterviewMeetings im on irfCI.IRFId = im.IRFId     
	left join InterviewComments ic on ic.InterviewId= im.id     
	inner join UserList ul on ul.EmailId = im.candidateEmail     
	inner join CandidatesMaster cm on ul.id =cm.UserId    
	inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irfCI.id)
	join @TemporaryCandidateTableVariable tmpC on tmpC.id =ca.id
	where im.IRFId = irf.IRFId and ic.MeetingStatus=4 and im.confirmationStatus=1 ) as CompletedInterviews,
	(select count(im.id) from irf as irfSM left join InterviewMeetings im on irfSM.IRFId= im.IRFId     
	left join InterviewComments ic on im.id= ic.InterviewId    
	left join QCFeedbackReport qcf on qcf.InterviewId = im.id    
	inner join UserList ul on ul.EmailId = im.candidateEmail     
	inner join CandidatesMaster cm on ul.id =cm.UserId    
	inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irfSM.id) 
	join @TemporaryCandidateTableVariable tmpC on tmpC.id =ca.id
	where im.IRFId = irf.IRFId and (ic.InterviewStatus= 2 or ic.InterviewStatus=4) and qcf.InterviewId is not null) as SelectedMeetings,
	(select count(im.id) from irf as irfRM left join InterviewMeetings im on irfRM.IRFId= im.IRFId     
	left join InterviewComments ic on im.id= ic.InterviewId    
	inner join UserList ul on ul.EmailId = im.candidateEmail     
	inner join CandidatesMaster cm on ul.id =cm.UserId    
	inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irfRM.id)  
	join @TemporaryCandidateTableVariable tmpC on tmpC.id =ca.id
	where im.IRFId = irf.IRFId and ic.InterviewStatus= 3) as RejectedMeetings,
	dbo.fn_GetNotScheduledCountForIRF(irf.id, @TemporaryCandidateTableVariable) as NotScheduledCount,
	dbo.fn_GetReScheduledCountForIRF(irf.id, @TemporaryCandidateTableVariable) as ReScheduledCount

	from IRF
	join Client cl on cl.id = irf.ClientId
	join UserList ul on ul.id = irf.RisebirdHr
	
	left join UserList ulb on ulb.id = irf.BillingOwner
	join @tbl_Temp tmp on tmp.IRFId =IRF.id
    

    
  
End    
  




GO

