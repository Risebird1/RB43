
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_GetReScheduledCountForIRF]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_GetReScheduledCountForIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_GetReScheduledCountForIRF(@irfId int,  @TemporaryCandidateTableVariable dbo.tempCandidate_TableType READONLY)
Returns int
as
Begin

declare @totalCount int
set @totalCount=0
--set @totalCount = 
select @totalCount = count(a.imid) from 
(Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,
cl.Name as ClientName from (Select imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn          
from 
(Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,
(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
ul.MobileNumber,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
from InterviewMeetings im 
join InterviewComments ic on ic.InterviewId=im.id         
join IRF on im.IRFId=IRF.IRFId
left join UserList ul on ul.EmailId=im.candidateEmail   
join CandidatesMaster cm on cm.UserId = ul.id
join Candidate ca on (ca.IRFId = irf.id and ca.CandidateMasterId = cm.id)
join @TemporaryCandidateTableVariable tempC on ca.id = tempC.id
where IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0  and irf.id = @irfId      
union      
Select distinct IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,
(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,        
ul.MobileNumber,im.ClientHrContact as ClientHrContact,(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus         
from InterviewMeetings im 
left join InterviewComments ic on ic.InterviewId=im.id         
join IRF on im.IRFId=IRF.IRFId         
left join UserList ul on ul.EmailId=im.candidateEmail
join CandidatesMaster cm on cm.UserId = ul.id
join Candidate ca on (ca.IRFId = irf.id and ca.CandidateMasterId = cm.id)
join @TemporaryCandidateTableVariable tempC on ca.id = tempC.id
where IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and irf.id = @irfId ) as cd       
where MeetingStatus=1 or MeetingStatus=3 or confirmationStatus=2     
group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber)as xy         
join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail      
join InterviewComments ic on im.id=ic.interviewid    
join IRF on im.IRFId=IRF.IRFId  
join Client cl on cl.id=IRF.ClientId
where IRF.id =@irfId) a

 return @totalCount

End