IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMarkedColdCandidate'))
BEGIN
    DROP PROCEDURE usp_GetMarkedColdCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetMarkedColdCandidate]   
@Emailid varchar(200),
@startDate datetime = NULL,
@endDate datetime = NULL
AS
Begin
Declare @Role varchar(150)              
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid  
IF(@Role='SeniorHR')
Begin
With CTE AS(
select cci.FKCandidateId,cci.IsColdCandidate,cci.ColdMarkedDate,cci.ColdMarkedBY ,cci.ColdSource,cci.ColdReason,c.IRFId as irfIntId,ul.EmailId as candidateEmail,IRF.IRFId,
cm.id as CandidateMasterId,c.id as CandidateId,ulir.EmailId as ChannelOwner,ul.MobileNumber,cl.Name as ClientName,case 
	 when ul.LastName is null then ul.FirstName
	 else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
end as candidateName
,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,LEN(IRF.CalendlyLink)) as scheduleUniqueid
from ColdCandidateInfos cci
join
Candidate c on cci.FKCandidateId=c.id
Join CandidatesMaster cm on c.CandidateMasterId=cm.id
join UserList ul on cm.UserId=ul.id
join IRF on c.IRFId=IRF.id 
left join Userlist ulir on ulir.id=irf.RisebirdHr
join Client cl on cl.id=IRF.ClientId
where cci.IsColdCandidate=1 and ulir.EmailId=@Emailid
and (@startDate is null or cci.ColdMarkedDate >= CAST(@startDate as date))
and (@endDate is null or cci.ColdMarkedDate < DATEADD(day,1,CAST(@endDate as date))))
Select CTE.*
,(select max(InterviewDate) from InterviewMeetings where candidateEmail=CTE.candidateEmail and  IRFId=CTE.IRFId) as interviewDate
,(SELECT top 1 ic.MeetingStatus
FROM InterviewMeetings im
JOIN InterviewComments ic ON im.id = ic.InterviewId
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId =CTE.IRFId order by im.interviewDate desc) as MeetingStatus
,(SELECT top 1 im.id
FROM InterviewMeetings im
--JOIN InterviewComments ic ON im.id = ic.InterviewId
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId=CTE.IRFId order by im.interviewDate desc) as imid
,CountTable.MailCount,LastCandidateStatus.StatusValue,LastCandidateStatus.LatestComment
from CTE
OUTER APPLY dbo.fn_GetCandidateCommunicationCountForIRF(CTE.irfIntId,CTE.candidateEmail) as CountTable
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.irfIntId and CandidateId = CTE.CandidateMasterId order by cs.Date desc) as LastCandidateStatus
order by interviewDate desc
End
Else
Begin
With CTE AS(
select cci.FKCandidateId,cci.IsColdCandidate,cci.ColdMarkedDate,cci.ColdMarkedBY ,cci.ColdSource,cci.ColdReason,c.IRFId as irfIntId,ul.EmailId as candidateEmail,IRF.IRFId,
cm.id as CandidateMasterId,c.id as CandidateId,ulir.EmailId as ChannelOwner,ul.MobileNumber,cl.Name as ClientName,case 
	 when ul.LastName is null then ul.FirstName
	 else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
end as candidateName
,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,LEN(IRF.CalendlyLink)) as scheduleUniqueid
from ColdCandidateInfos cci
join
Candidate c on cci.FKCandidateId=c.id
Join CandidatesMaster cm on c.CandidateMasterId=cm.id
join UserList ul on cm.UserId=ul.id
join IRF on c.IRFId=IRF.id 
left join Userlist ulir on ulir.id=irf.RisebirdHr
join Client cl on cl.id=IRF.ClientId
where cci.IsColdCandidate=1
and (@startDate is null or cci.ColdMarkedDate >= CAST(@startDate as date))
and (@endDate is null or cci.ColdMarkedDate < DATEADD(day,1,CAST(@endDate as date))))
Select CTE.*
,(select max(InterviewDate) from InterviewMeetings where candidateEmail=CTE.candidateEmail and  IRFId=CTE.IRFId) as interviewDate
,(SELECT top 1 ic.MeetingStatus
FROM InterviewMeetings im
JOIN InterviewComments ic ON im.id = ic.InterviewId
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId =CTE.IRFId order by im.interviewDate desc) as MeetingStatus
,(SELECT top 1 im.id
FROM InterviewMeetings im
--JOIN InterviewComments ic ON im.id = ic.InterviewId
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId=CTE.IRFId order by im.interviewDate desc) as imid
,CountTable.MailCount,LastCandidateStatus.StatusValue,LastCandidateStatus.LatestComment
from CTE
OUTER APPLY dbo.fn_GetCandidateCommunicationCountForIRF(CTE.irfIntId,CTE.candidateEmail) as CountTable
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.irfIntId and CandidateId = CTE.CandidateMasterId order by cs.Date desc) as LastCandidateStatus
order by interviewDate desc
End
End
GO







