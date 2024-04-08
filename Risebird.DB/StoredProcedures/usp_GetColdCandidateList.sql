IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetColdCandidateList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetColdCandidateList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].usp_GetColdCandidateList  
As
Begin 
;With CTE AS(SELECT im.candidateEmail, im.IRFId
FROM InterviewMeetings im
JOIN InterviewComments ic ON im.id = ic.InterviewId
JOIN IRF ON im.IRFId = IRF.IRFId
GROUP BY im.candidateEmail, im.IRFId
HAVING COUNT(im.candidateEmail) >= 5
AND NOT EXISTS (
    SELECT 1
    FROM InterviewComments ic2
    JOIN InterviewMeetings im2 ON ic2.InterviewId = im2.id
    WHERE im.candidateEmail = im2.candidateEmail
    AND ic2.MeetingStatus in (4,6,7,8)
)
)
Select ca.id,CTE.candidateEmail,CTE.IRFId ,ca.IRFId as irfIntId
,(select max(InterviewDate) from InterviewMeetings where candidateEmail=CTE.candidateEmail and  IRFId=CTE.IRFId) as interviewDate
,(SELECT top 1 ic.MeetingStatus
FROM InterviewMeetings im
JOIN InterviewComments ic ON im.id = ic.InterviewId
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId =CTE.IRFId order by im.interviewDate desc) as MeetingStatus
--,CountTable.MailCount
,cm.id as CandidateMasterId,
case 
	 when ul.LastName is null then ul.FirstName
	 else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
end as candidateName
,(SELECT top 1 im.id
FROM InterviewMeetings im
--JOIN InterviewComments ic ON im.id = ic.InterviewId
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId=CTE.IRFId order by im.interviewDate desc) as imid
,cl.Name as ClientName,CountTable.MailCount
,cci.IsColdCandidate,cci.ColdMarkedBY,cci.ColdMarkedDate,ca.id as CandidateId,ulir.EmailId as ChannelOwner,ul.MobileNumber
from Candidate ca join IRF on ca.IRFId=IRF.id    
join CandidatesMaster cm on ca.CandidateMasterId=cm.id      
join UserList ul on ul.id = cm.UserId 
Join CTE on ul.EmailId= CTE.candidateEmail and IRF.IRFId=CTE.IRFId
left Join ColdCandidateInfos cci on ca.id=cci.FKCandidateId
left join Userlist ulir on ulir.id=irf.RisebirdHr
join Client cl on cl.id=IRF.ClientId 
OUTER APPLY dbo.fn_GetCandidateCommunicationCountForIRF(IRF.id,CTE.candidateEmail) as CountTable
where cci.IsColdCandidate is null or IsColdCandidate=0
order by interviewDate desc
End
Go