/****** Object:  StoredProcedure [dbo].[usp_GetCandidateLastStatus]    Script Date: 27-01-2021 11.58.12 AM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateLastStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateLastStatus
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create PROCEDURE [dbo].[usp_GetCandidateLastStatus]  
@dateValue datetime
AS  
Begin 

declare @meetingTable table (imid int, IRFId int, CandidateId int);
		insert into @meetingTable
		select im.id, irf.id, cm.id from InterviewMeetings im 
		join IRF on irf.IRFId = im.IRFId
		join UserList ul on ul.EmailId = im.candidateEmail
		join CandidatesMaster cm on cm.UserId = ul.id
		where im.id in (select max(id) from InterviewMeetings where confirmationStatus=1 group by IRFId, candidateEmail)

-- the last status of the candidate 
select cs.IRFId as IRFintId, cs.Comments, csom.StatusValue, irf.IRFId, cs.[Date] as PerformedOn, ul.EmailId as candidateEmail, ul.FirstName as candidateName, qcfr.QCStatus, IRF.JDTiltle as JDTitle from CandidateStatus cs join CandidateStatusOptionsMst csom on csom.id=cs.[Status] 
join IRF on irf.id = cs.IRFId
join CandidatesMaster cm on cm.id = cs.CandidateId
join UserList ul on ul.id = cm.UserId
left join @meetingTable mt on  (mt.IRFId=cs.IRFId and mt.CandidateId=cs.CandidateId)
left join InterviewComments ic on ic.InterviewId = mt.imid
left join QCFeedbackReport qcfr on qcfr.InterviewId= ic.InterviewId
where cs.id in (select max(id) from CandidateStatus group by CandidateId, IRFId) and cast(cs.[Date] as date) =  cast (@dateValue as date)


End

GO


