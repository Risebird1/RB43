IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMarkedColdCandidateByClient'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMarkedColdCandidateByClient
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetMarkedColdCandidateByClient]         
@Email varchar(100)    
As    
Begin    
Declare @RoleId int,@Clientid int    
set @Clientid= (Select cu.ClientId from UserList ul join ClientUser cu on cu.UserId=ul.id where ul.EmailId=@Email)          
set @RoleId =(select top 1 cu.RoleId from UserList ul join ClientUser cu on cu.UserId = ul.id where ul.EmailId =@Email)                  
    
if(@RoleId = 4)     
begin           
With CTE AS(    
select cci.FKCandidateId,cci.IsColdCandidate,cci.ColdMarkedDate,cci.ColdMarkedBY ,cci.ColdSource,cci.ColdReason,c.IRFId as irfIntId,ul.EmailId as candidateEmail,IRF.IRFId,    
cm.id as CandidateMasterId,c.id as CandidateId,ulir.EmailId as ChannelOwner,ul.MobileNumber,cl.Name as ClientName,case     
  when ul.LastName is null then ul.FirstName    
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))    
end as candidateName,ulc.EmailId ClientEmail    
,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,LEN(IRF.CalendlyLink)) as scheduleUniqueid,
projectdetails.ProjectTitle,projectdetails.ProjectUniqueid,ulh.FirstName as HRFirstName,ulh.LastName as HRLastName 
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
left join UserList ulh on ulh.EmailId=c.ClientHRAttemptContact
outer apply(Select pr.ProjectUniqueid,pr.ProjectTitle From ProjectIRFMapping pim join Project pr on pr.id=pim.ProjectId where pim.IRFId=IRF.id) as projectdetails
where cci.IsColdCandidate=1    
and ulc.EmailId=@Email and c.ProfileSource=@Email)    
Select CTE.*    
,(select max(InterviewDate) from InterviewMeetings where candidateEmail=CTE.candidateEmail and  IRFId=CTE.IRFId) as interviewDate    
,(SELECT top 1 ic.MeetingStatus    
FROM InterviewMeetings im    
JOIN InterviewComments ic ON im.id = ic.InterviewId    
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId =CTE.IRFId order by im.interviewDate desc) as MeetingStatus    
,(SELECT top 1 im.id    
FROM InterviewMeetings im    
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId=CTE.IRFId order by im.interviewDate desc) as imid    
,LastCandidateStatus.StatusValue,LastCandidateStatus.LatestComment,dbo.fn_HasEmailDroppedOrBounced(CTE.candidateEmail) as HasEmailDroppedOrBounced,CandidateAddStatus.Date as CandidateAddedOn    
from CTE    
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.irfIntId and CandidateId = CTE.CandidateMasterId order by cs.Date desc) as LastCandidateStatus    
outer apply(select top 1 cs.Date From CandidateStatus cs where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and cs.Status=12 order by cs.Date desc) as CandidateAddStatus      
order by interviewDate desc    
END    
else if(@RoleId = 1)     
begin        
With CTE AS(    
select cci.FKCandidateId,cci.IsColdCandidate,cci.ColdMarkedDate,cci.ColdMarkedBY ,cci.ColdSource,cci.ColdReason,c.IRFId as irfIntId,ul.EmailId as candidateEmail,IRF.IRFId,    
cm.id as CandidateMasterId,c.id as CandidateId,ulir.EmailId as ChannelOwner,ul.MobileNumber,cl.Name as ClientName,case     
  when ul.LastName is null then ul.FirstName    
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))    
end as candidateName,ulc.EmailId ClientEmail    
,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,LEN(IRF.CalendlyLink)) as scheduleUniqueid,
projectdetails.ProjectTitle,projectdetails.ProjectUniqueid,ulh.FirstName as HRFirstName,ulh.LastName as HRLastName 
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
left join UserList ulh on ulh.EmailId=c.ClientHRAttemptContact
outer apply(Select pr.ProjectUniqueid,pr.ProjectTitle From ProjectIRFMapping pim join Project pr on pr.id=pim.ProjectId where pim.IRFId=IRF.id) as projectdetails
where cci.IsColdCandidate=1    
and ulc.EmailId=@Email and c.ClientHRAttemptContact=@Email)    
Select CTE.*    
,(select max(InterviewDate) from InterviewMeetings where candidateEmail=CTE.candidateEmail and  IRFId=CTE.IRFId) as interviewDate    
,(SELECT top 1 ic.MeetingStatus    
FROM InterviewMeetings im  JOIN InterviewComments ic ON im.id = ic.InterviewId    
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId =CTE.IRFId order by im.interviewDate desc) as MeetingStatus    
,(SELECT top 1 im.id    
FROM InterviewMeetings im    
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId=CTE.IRFId order by im.interviewDate desc) as imid    
,LastCandidateStatus.StatusValue,LastCandidateStatus.LatestComment,dbo.fn_HasEmailDroppedOrBounced(CTE.candidateEmail) as HasEmailDroppedOrBounced,CandidateAddStatus.Date as CandidateAddedOn    
from CTE    
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.irfIntId and CandidateId = CTE.CandidateMasterId order by cs.Date desc) as LastCandidateStatus    
outer apply(select top 1 cs.Date From CandidateStatus cs where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and cs.Status=12 order by cs.Date desc) as CandidateAddStatus      
order by interviewDate desc    
END    
else if(@RoleId = 9)     
begin      
With CTE AS(    
select cci.FKCandidateId,cci.IsColdCandidate,cci.ColdMarkedDate,cci.ColdMarkedBY ,cci.ColdSource,cci.ColdReason,c.IRFId as irfIntId,ul.EmailId as candidateEmail,IRF.IRFId,    
cm.id as CandidateMasterId,c.id as CandidateId,ulir.EmailId as ChannelOwner,ul.MobileNumber,cl.Name as ClientName,case     
  when ul.LastName is null then ul.FirstName    
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))    
end as candidateName,  
SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,LEN(IRF.CalendlyLink)) as scheduleUniqueid,
projectdetails.ProjectTitle,projectdetails.ProjectUniqueid,ulh.FirstName as HRFirstName,ulh.LastName as HRLastName 
from ColdCandidateInfos cci    
join    
Candidate c on cci.FKCandidateId=c.id    
Join CandidatesMaster cm on c.CandidateMasterId=cm.id    
join UserList ul on cm.UserId=ul.id    
join IRF on c.IRFId=IRF.id     
left join Userlist ulir on ulir.id=irf.RisebirdHr    
join Client cl on cl.id=IRF.ClientId    
left join UserList ulh on ulh.EmailId=c.ClientHRAttemptContact
outer apply(Select pr.ProjectUniqueid,pr.ProjectTitle From ProjectIRFMapping pim join Project pr on pr.id=pim.ProjectId where pim.IRFId=IRF.id) as projectdetails
where cci.IsColdCandidate=1 and IRF.ClientId=@Clientid)    
Select CTE.*    
,(select max(InterviewDate) from InterviewMeetings where candidateEmail=CTE.candidateEmail and  IRFId=CTE.IRFId) as interviewDate    
,(SELECT top 1 ic.MeetingStatus    
FROM InterviewMeetings im    
JOIN InterviewComments ic ON im.id = ic.InterviewId    
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId =CTE.IRFId order by im.interviewDate desc) as MeetingStatus    
,(SELECT top 1 im.id    
FROM InterviewMeetings im    
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId=CTE.IRFId order by im.interviewDate desc) as imid    
,LastCandidateStatus.StatusValue,LastCandidateStatus.LatestComment,dbo.fn_HasEmailDroppedOrBounced(CTE.candidateEmail) as HasEmailDroppedOrBounced,CandidateAddStatus.Date as CandidateAddedOn    
from CTE    
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.irfIntId and CandidateId = CTE.CandidateMasterId order by cs.Date desc) as LastCandidateStatus    
outer apply(select top 1 cs.Date From CandidateStatus cs where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and cs.Status=12 order by cs.Date desc) as CandidateAddStatus      
order by interviewDate desc    
END    
else     
begin    
With CTE AS(    
select cci.FKCandidateId,cci.IsColdCandidate,cci.ColdMarkedDate,cci.ColdMarkedBY ,cci.ColdSource,cci.ColdReason,c.IRFId as irfIntId,ul.EmailId as candidateEmail,IRF.IRFId,    
cm.id as CandidateMasterId,c.id as CandidateId,ulir.EmailId as ChannelOwner,ul.MobileNumber,cl.Name as ClientName,case     
  when ul.LastName is null then ul.FirstName    
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))    
end as candidateName,ulc.EmailId ClientEmail    
,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,LEN(IRF.CalendlyLink)) as scheduleUniqueid,
projectdetails.ProjectTitle,projectdetails.ProjectUniqueid,ulh.FirstName as HRFirstName,ulh.LastName as HRLastName 
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
left join UserList ulh on ulh.EmailId=c.ClientHRAttemptContact
outer apply(Select pr.ProjectUniqueid,pr.ProjectTitle From ProjectIRFMapping pim join Project pr on pr.id=pim.ProjectId where pim.IRFId=IRF.id) as projectdetails
where cci.IsColdCandidate=1 and ulc.EmailId=@Email)    
Select CTE.*    
,(select max(InterviewDate) from InterviewMeetings where candidateEmail=CTE.candidateEmail and  IRFId=CTE.IRFId) as interviewDate    
,(SELECT top 1 ic.MeetingStatus    
FROM InterviewMeetings im    
JOIN InterviewComments ic ON im.id = ic.InterviewId    
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId =CTE.IRFId order by im.interviewDate desc) as MeetingStatus    
,(SELECT top 1 im.id    
FROM InterviewMeetings im    
JOIN IRF ON im.IRFId = IRF.IRFId where im.candidateEmail =CTE.candidateEmail and im.IRFId=CTE.IRFId order by im.interviewDate desc) as imid    
,LastCandidateStatus.StatusValue,LastCandidateStatus.LatestComment,dbo.fn_HasEmailDroppedOrBounced(CTE.candidateEmail) as HasEmailDroppedOrBounced,CandidateAddStatus.Date as CandidateAddedOn    
from CTE    
outer apply(select top 1 csom.StatusValue,cs.Comments as LatestComment From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=CTE.irfIntId and CandidateId = CTE.CandidateMasterId order by cs.Date desc) as LastCandidateStatus    
outer apply(select top 1 cs.Date From CandidateStatus cs where cs.IRFId=CTE.IRFId and CandidateId = CTE.CandidateMasterId and cs.Status=12 order by cs.Date desc) as CandidateAddStatus      
order by interviewDate desc    
END    
End    
GO