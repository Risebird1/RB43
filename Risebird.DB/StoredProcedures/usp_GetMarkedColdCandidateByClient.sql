IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMarkedColdCandidateByClient'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMarkedColdCandidateByClient
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetMarkedColdCandidateByClient]     
@Email varchar(100)
As
Begin
With CTE AS(
select cci.FKCandidateId,cci.IsColdCandidate,cci.ColdMarkedDate,cci.ColdMarkedBY ,c.IRFId as irfIntId,ul.EmailId as candidateEmail,IRF.IRFId,
cm.id as CandidateMasterId,c.id as CandidateId,ulir.EmailId as ChannelOwner,ul.MobileNumber,cl.Name as ClientName,case 
	 when ul.LastName is null then ul.FirstName
	 else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
end as candidateName,ulc.EmailId ClientEmail
from ColdCandidateInfos cci
join
Candidate c on cci.FKCandidateId=c.id
Join CandidatesMaster cm on c.CandidateMasterId=cm.id
join UserList ul on cm.UserId=ul.id
join IRF on c.IRFId=IRF.id 
left join Userlist ulir on ulir.id=irf.RisebirdHr
join Client cl on cl.id=IRF.ClientId
join TransClientUser tcu on IRF.id=tcu.IRFId
join UserList ulc on tcu.ClientUserId=ulc.id 
where cci.IsColdCandidate=1
and ulc.EmailId=@Email
)
Select CTE.*
,(select max(InterviewDate) from InterviewMeetings where candidateEmail=CTE.candidateEmail and  IRFId=CTE.IRFId) as interviewDate
,(SELECT top 1 ic.MeetingStatus
FROM InterviewMeetings im
JOIN InterviewComments ic ON im.id = ic.InterviewId
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId =CTE.IRFId order by im.interviewDate desc) as MeetingStatus
,(SELECT top 1 im.id
FROM InterviewMeetings im
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId=CTE.IRFId order by im.interviewDate desc) as imid
,CountTable.MailCount,CountTable.CallsCount,CountTable.WhatsappCount
from CTE
OUTER APPLY dbo.fn_GetCandidateCommunicationCountForIRF(CTE.irfIntId,CTE.candidateEmail) as CountTable
order by interviewDate desc
End
GO