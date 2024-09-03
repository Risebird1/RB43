
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientReScheduledMeetings'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetClientReScheduledMeetings]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_GetClientReScheduledMeetings        
@Clientid int,                            
@Emailid varchar(150)                            
AS                                
Begin    
if(@Clientid>0)                        
Begin                      
              
declare @clientRole int              
set @clientRole =(select top 1 cu.RoleId from UserList ul join ClientUser cu on cu.UserId = ul.id where ul.EmailId =@Emailid)              
              
if(@clientRole=4)--staffing Agency              
begin              
 Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName,CandidateStatus.StatusValue from   
 (Select distinct imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn,JobCountry, CandidateMasterId,cd.ChannelOwner,CandidateId,InterviewId           
 from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,  
 IRF.JDTiltle,                      
 ul.MobileNumber,im.resumeLink,          
 --im.ClientHrContact as ClientHrContact,          
  c.ClientHRAttemptContact as ClientHrContact,          
 (im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId,ulir.EmailId as ChannelOwner,c.id as CandidateId                       
 ,im.id as InterviewId  
 from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id                       
 join IRF on im.IRFId=IRF.IRFId                       
 left join UserList ul on ul.EmailId=im.candidateEmail              
 join CandidatesMaster cm on cm.UserId=ul.id              
 join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id               
 join TransClientUser tcu on tcu.IRFId=IRF.id                            
 left join Userlist tcul on tcul.id=tcu.ClientUserId  
 left join Userlist ulir on ulir.id=irf.RisebirdHr  
 left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
 where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and c.ProfileSource=@Emailid and ISNULL(cci.IsColdCandidate,0) !=1               
 union                    
 Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
 ul.MobileNumber,im.resumeLink,          
  --im.ClientHrContact as ClientHrContact,          
  c.ClientHRAttemptContact as ClientHrContact,          
 (im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId ,ulir.EmailId as ChannelOwner,c.id as CandidateId                                      
 ,im.id as InterviewId  
 from InterviewMeetings im left join InterviewComments ic on ic.InterviewId=im.id                       
 join IRF on im.IRFId=IRF.IRFId                       
 left join UserList ul on ul.EmailId=im.candidateEmail              
 join CandidatesMaster cm on cm.UserId=ul.id              
 join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id              
 join TransClientUser tcu on tcu.IRFId=IRF.id                            
 left join Userlist tcul on tcul.id=tcu.ClientUserId  
 left join Userlist ulir on ulir.id=irf.RisebirdHr  
 left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
 where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and c.ProfileSource=@Emailid and ISNULL(cci.IsColdCandidate,0) !=1 ) as cd                     
 group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,JobCountry, CandidateMasterId,ChannelOwner,CandidateId,InterviewId)as xy                       
 join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail and im.IRFId=xy.IRFId                    
 join InterviewComments ic on im.id=ic.interviewid          
 join IRF on im.IRFId=IRF.IRFId                
 join Client cl on cl.id=IRF.ClientId            
 outer apply(select top 1 csom.StatusValue From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=xy.imid and CandidateId = xy.CandidateMasterId order by cs.Date desc) as CandidateStatus  
 where MeetingStatus=1 or MeetingStatus=3 or MeetingStatus=5 or MeetingStatus=9 or confirmationStatus=2      
 order by dtCreatedOn desc               
end              
else if(@clientRole=1)          
begin              
  Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName,CandidateStatus.StatusValue from   
  (Select distinct imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn,JobCountry, CandidateMasterId,cd.ChannelOwner,CandidateId,InterviewId      
        
 from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,  
 IRF.JDTiltle,                      
 ul.MobileNumber,im.resumeLink,          
  --im.ClientHrContact as ClientHrContact,          
  c.ClientHRAttemptContact as ClientHrContact,          
 (im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId,ulir.EmailId as ChannelOwner ,c.id as CandidateId                      
 ,im.id as InterviewId  
 from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id                       
 join IRF on im.IRFId=IRF.IRFId                 
 left join UserList ul on ul.EmailId=im.candidateEmail              
 join CandidatesMaster cm on cm.UserId=ul.id              
 join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id join TransClientUser tcu on tcu.IRFId=IRF.id                            
 left join Userlist tcul on tcul.id=tcu.ClientUserId    
 left join Userlist ulir on ulir.id=irf.RisebirdHr  
 left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
 where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and im.ClientHrContact=@Emailid AND ISNULL(cci.IsColdCandidate,0) !=1                     
 union                    
 Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
 ul.MobileNumber,im.resumeLink,          
 --im.ClientHrContact as ClientHrContact,          
 c.ClientHRAttemptContact as ClientHrContact,          
 (im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId,ulir.EmailId as ChannelOwner,c.id as CandidateId                     
 ,im.id as InterviewId  
 from InterviewMeetings im left join InterviewComments ic on ic.InterviewId=im.id                       
 join IRF on im.IRFId=IRF.IRFId                       
 left join UserList ul on ul.EmailId=im.candidateEmail              
 join CandidatesMaster cm on cm.UserId=ul.id              
 join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                      
 join TransClientUser tcu on tcu.IRFId=IRF.id                            
 left join Userlist tcul on tcul.id=tcu.ClientUserId   
 left join Userlist ulir on ulir.id=irf.RisebirdHr  
 left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
 where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and im.ClientHrContact=@Emailid AND ISNULL(cci.IsColdCandidate,0) !=1) as cd                     
 group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,JobCountry, CandidateMasterId,ChannelOwner,CandidateId,InterviewId)as xy                       
 join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail and im.IRFId=xy.IRFId                    
 join InterviewComments ic on im.id=ic.interviewid                
 join IRF on im.IRFId=IRF.IRFId                
 join Client cl on cl.id=IRF.ClientId    
 outer apply(select top 1 csom.StatusValue From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=xy.imid and CandidateId = xy.CandidateMasterId order by cs.Date desc) as CandidateStatus  
 where MeetingStatus=1 or MeetingStatus=3 or MeetingStatus=5 or MeetingStatus=9 or confirmationStatus=2       
 order by dtCreatedOn desc               
end            
else if(@clientRole=9)          
begin              
  Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName,CandidateStatus.StatusValue from   
  (Select distinct imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn,JobCountry, CandidateMasterId ,cd.ChannelOwner,CandidateId,InterviewId     
from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
 ul.MobileNumber,im.resumeLink,c.ClientHRAttemptContact as ClientHrContact,          
 (im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId ,ulir.EmailId as ChannelOwner ,c.id as CandidateId                     
 ,im.id as InterviewId  
 from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id                       
 join IRF on im.IRFId=IRF.IRFId                 
 left join UserList ul on ul.EmailId=im.candidateEmail              
 join CandidatesMaster cm on cm.UserId=ul.id              
 join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id join ClientUser tcu on tcu.ClientId=IRF.ClientId                            
 left join Userlist tcul on tcul.id=tcu.UserId  
 left join Userlist ulir on ulir.id=irf.RisebirdHr  
 left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
 where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1   
 union                    
 Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
 ul.MobileNumber,im.resumeLink,          
 c.ClientHRAttemptContact as ClientHrContact,          
 (im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId ,ulir.EmailId as ChannelOwner ,c.id as CandidateId                     
 ,im.id as InterviewId  
 from InterviewMeetings im left join InterviewComments ic on ic.InterviewId=im.id                       
 join IRF on im.IRFId=IRF.IRFId                       
 left join UserList ul on ul.EmailId=im.candidateEmail              
 join CandidatesMaster cm on cm.UserId=ul.id              
 join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                      
 join ClientUser tcu on tcu.ClientId=IRF.ClientId                            
 left join Userlist tcul on tcul.id=tcu.UserId   
 left join Userlist ulir on ulir.id=irf.RisebirdHr  
 left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
 where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and im.ClientHrContact=@Emailid and ISNULL(cci.IsColdCandidate,0) !=1) as cd                     
 group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,JobCountry, CandidateMasterId,ChannelOwner,CandidateId,InterviewId)as xy                       
 join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail and im.IRFId=xy.IRFId                    
 join InterviewComments ic on im.id=ic.interviewid                
 join IRF on im.IRFId=IRF.IRFId                
 join Client cl on cl.id=IRF.ClientId     
 outer apply(select top 1 csom.StatusValue From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=xy.imid and CandidateId = xy.CandidateMasterId order by cs.Date desc) as CandidateStatus  
 where MeetingStatus=1 or MeetingStatus=3 or MeetingStatus=5 or MeetingStatus=9 or confirmationStatus=2       
 order by dtCreatedOn desc               
end            
else              
begin              
 Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName,CandidateStatus.StatusValue  
 from (Select distinct imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn,JobCountry, CandidateMasterId,cd.ChannelOwner,CandidateId ,InterviewId          
 from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
 ul.MobileNumber,im.resumeLink,          
 --im.ClientHrContact as ClientHrContact,          
 c.ClientHRAttemptContact as ClientHrContact,          
 (im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId,ulir.EmailId as ChannelOwner,c.id as CandidateId                      
 ,im.id as InterviewId  
 from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id                       
 join IRF on im.IRFId=IRF.IRFId                       
 left join UserList ul on ul.EmailId=im.candidateEmail              
 join CandidatesMaster cm on cm.UserId=ul.id              
 join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                      
 join TransClientUser tcu on tcu.IRFId=IRF.id                            
 left join Userlist tcul on tcul.id=tcu.ClientUserId    
 left join Userlist ulir on ulir.id=irf.RisebirdHr  
 left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
 where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1                  
 union                    
 Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
 ul.MobileNumber,im.resumeLink,          
 --im.ClientHrContact as ClientHrContact,          
 c.ClientHRAttemptContact as ClientHrContact,          
 (im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId,ulir.EmailId as ChannelOwner,c.id as CandidateId                      
 ,im.id as InterviewId  
 from InterviewMeetings im left join InterviewComments ic on ic.InterviewId=im.id                       
 join IRF on im.IRFId=IRF.IRFId                       
 left join UserList ul on ul.EmailId=im.candidateEmail              
 join CandidatesMaster cm on cm.UserId=ul.id              
 join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                      
 join TransClientUser tcu on tcu.IRFId=IRF.id                            
 left join Userlist tcul on tcul.id=tcu.ClientUserId  
 left join Userlist ulir on ulir.id=irf.RisebirdHr  
 left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
 where tcul.EmailId=@Emailid and irf.ClientId=@Clientid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1)  as cd                     
 group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,JobCountry, CandidateMasterId,ChannelOwner,CandidateId,InterviewId)as xy                       
 join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail and im.IRFId=xy.IRFId                    
 join InterviewComments ic on im.id=ic.interviewid                
 join IRF on im.IRFId=IRF.IRFId                
 join Client cl on cl.id=IRF.ClientId     
 outer apply(select top 1 csom.StatusValue From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=xy.imid and CandidateId = xy.CandidateMasterId order by cs.Date desc) as CandidateStatus  
 where MeetingStatus=1 or MeetingStatus=3 or MeetingStatus=5 or MeetingStatus=9 or confirmationStatus=2         
 order by dtCreatedOn desc               
end              
                    
                      
                    
End                      
Else                      
Begin                      
Declare @Role int                        
Select @Role=idProfile from Userlist where Emailid=@Emailid                        
if(@Role=1 or @Role=31 or @Role=32)                        
Begin                      
                    
Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName,CandidateStatus.StatusValue  
from (Select imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn,JobCountry, CandidateMasterId ,cd.ChannelOwner,CandidateId,InterviewId                     
from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,  
IRF.JDTiltle,                      
ul.MobileNumber,          
--im.ClientHrContact as ClientHrContact,          
c.ClientHRAttemptContact as ClientHrContact,          
(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId ,ulir.EmailId as ChannelOwner,c.id as CandidateId  
,im.id as InterviewId  
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id                       
join IRF on im.IRFId=IRF.IRFId                       
left join UserList ul on ul.EmailId=im.candidateEmail              
join CandidatesMaster cm on cm.UserId=ul.id              
join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id       
left join Userlist ulir on ulir.id=irf.RisebirdHr  
left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
where IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1                     
union                    
Select distinct IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,  
IRF.JDTiltle,                      
ul.MobileNumber,          
--im.ClientHrContact as ClientHrContact,          
c.ClientHRAttemptContact as ClientHrContact,          
(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId ,ulir.EmailId as ChannelOwner,c.id as CandidateId  
,im.id as InterviewId  
from InterviewMeetings im left join InterviewComments ic on ic.InterviewId=im.id                       
join IRF on im.IRFId=IRF.IRFId                       
left join UserList ul on ul.EmailId=im.candidateEmail              
join CandidatesMaster cm on cm.UserId=ul.id              
join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id   
left join Userlist ulir on ulir.id=irf.RisebirdHr  
left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
where IRF.EndDate>=GetDate()-60 and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1) as cd          
group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,JobCountry, CandidateMasterId,ChannelOwner,CandidateId,InterviewId)as xy                       
join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail                    
join InterviewComments ic on im.id=ic.interviewid                  
join IRF on im.IRFId=IRF.IRFId                
join Client cl on cl.id=IRF.ClientId     
outer apply(select top 1 csom.StatusValue From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=xy.imid and CandidateId = xy.CandidateMasterId order by cs.Date desc) as CandidateStatus  
where MeetingStatus=1 or MeetingStatus=3 or MeetingStatus=5 or MeetingStatus=9 or confirmationStatus=2       
order by dtCreatedOn desc           
                    
End                      
--Else if(@Role=31 or @Role=32)                        
--Begin                      
                    
--Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName,CandidateStatus.StatusValue   
--from (Select imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn,JobCountry, CandidateMasterId,cd.ChannelOwner,CandidateId,InterviewId                
--from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
--ul.MobileNumber,          
----im.ClientHrContact as ClientHrContact,          
--c.ClientHRAttemptContact as ClientHrContact,          
--(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId,ulir.EmailId as ChannelOwner,c.id as CandidateId,im.id as InterviewId  
--from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id                       
--join IRF on im.IRFId=IRF.IRFId                       
--join SeniorHr sh on sh.Clientid=IRF.ClientId            
--left join UserList ul on ul.EmailId=im.candidateEmail              
--join CandidatesMaster cm on cm.UserId=ul.id              
--join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id            
--left join UserList ulsh on ulsh.id=sh.Userid     
--left join Userlist ulir on ulir.id=irf.RisebirdHr  
--left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
--where ulsh.Emailid=@Emailid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1                     
--union                    
--Select distinct IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
--ul.MobileNumber,          
----im.ClientHrContact as ClientHrContact,          
--c.ClientHRAttemptContact as ClientHrContact,          
--(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId ,ulir.EmailId as ChannelOwner,c.id as CandidateId ,im.id as InterviewId                     
--from InterviewMeetings im left join InterviewComments ic on ic.InterviewId=im.id                       
--join IRF on im.IRFId=IRF.IRFId                       
--join SeniorHr sh on sh.Clientid=IRF.ClientId            
--left join UserList ul on ul.EmailId=im.candidateEmail              
--join CandidatesMaster cm on cm.UserId=ul.id              
--join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id               
--left join UserList ulsh on ulsh.id=sh.Userid  
--left join Userlist ulir on ulir.id=irf.RisebirdHr  
--left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
--where ulsh.Emailid=@Emailid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1) as cd                     
--group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,JobCountry, CandidateMasterId,ChannelOwner,CandidateId,InterviewId)as xy                       
--join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail                    
--join InterviewComments ic on im.id=ic.interviewid                  
--join IRF on im.IRFId=IRF.IRFId                
--join Client cl on cl.id=IRF.ClientId      
--outer apply(select top 1 csom.StatusValue From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=xy.imid and CandidateId = xy.CandidateMasterId order by cs.Date desc) as CandidateStatus  
--where MeetingStatus=1 or MeetingStatus=3 or MeetingStatus=5 or MeetingStatus=9 or confirmationStatus=2       
--order by dtCreatedOn desc                     
                    
--End                      
Else                   
Begin                      
declare @hrId int                          
set @hrId = (select HR.id from UserList ul join HR on HR.UserId=ul.id where EmailId=@Emailid and idProfile=2)                          
                    
Select xy.*,im.ClientHrContact,im.resumeLink,ic.MeetingStatus,im.confirmationStatus,irf.CalendlyLink,cl.Name as ClientName,CandidateStatus.StatusValue from   
(Select imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,MAX(dtCreatedOn) as dtCreatedOn,JobCountry , CandidateMasterId,cd.ChannelOwner,CandidateId,InterviewId                     
from (Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
ul.MobileNumber,im.resumeLink,          
--im.ClientHrContact as ClientHrContact,          
c.ClientHRAttemptContact as ClientHrContact,          
(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId,ulir.EmailId as ChannelOwner,c.id as CandidateId,im.id as InterviewId                       
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id                       
join IRF on im.IRFId=IRF.IRFId                       
left join UserList ul on ul.EmailId=im.candidateEmail              
join CandidatesMaster cm on cm.UserId=ul.id              
join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                      
join TransHR th on th.IRFId=irf.id  
left join Userlist ulir on ulir.id=irf.RisebirdHr  
left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
where th.HRId=@hrId and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1                    
union                      
Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
ul.MobileNumber,im.resumeLink,          
--im.ClientHrContact as ClientHrContact,          
c.ClientHRAttemptContact as ClientHrContact,          
(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId ,ulir.EmailId as ChannelOwner,c.id as CandidateId,im.id as InterviewId                      
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id                       
join IRF on im.IRFId=IRF.IRFId                       
left join UserList ul on ul.EmailId=im.candidateEmail              
join CandidatesMaster cm on cm.UserId=ul.id              
join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                      
left join Userlist ulir on ulir.id=irf.RisebirdHr  
left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
where ulir.EmailId=@Emailid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1                    
union                     
Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
ul.MobileNumber,im.resumeLink,          
--im.ClientHrContact as ClientHrContact,          
c.ClientHRAttemptContact as ClientHrContact,          
(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId ,ulir.EmailId as ChannelOwner,c.id as CandidateId ,im.id as InterviewId                    
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id                       
join IRF on im.IRFId=IRF.IRFId                       
left join UserList ul on ul.EmailId=im.candidateEmail              
join CandidatesMaster cm on cm.UserId=ul.id              
join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                      
join TransHR th on th.IRFId=irf.id   
left join Userlist ulir on ulir.id=irf.RisebirdHr   
left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
where th.HRId=@hrId and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0  and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1                   
union                      
Select IRF.id as imid,ul.Emailid as candidateEmail,im.IRFId,(ISNULL(ul.FirstName,'') +' '+ISNULL(ul.LastName,'')) as candidateName,IRF.JDTiltle,                      
ul.MobileNumber,im.resumeLink,          
--im.ClientHrContact as ClientHrContact,          
c.ClientHRAttemptContact as ClientHrContact,          
(im.interviewDate) as dtCreatedOn,ic.MeetingStatus,im.confirmationStatus,IRF.JobCountry, cm.id as CandidateMasterId,ulir.EmailId as ChannelOwner,c.id as CandidateId,im.id as InterviewId                       
from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id                       
join IRF on im.IRFId=IRF.IRFId                       
left join UserList ul on ul.EmailId=im.candidateEmail              
join CandidatesMaster cm on cm.UserId=ul.id              
join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id                      
left join Userlist ulir on ulir.id=irf.RisebirdHr   
left join ColdCandidateInfos  cci on c.id=cci.FKCandidateId  
where ulir.EmailId=@Emailid and IRF.EndDate>=GetDate() and IsNull(IsPaused,0)=0 and IsNull(IRF.IsDeleted,0)=0 and ISNULL(cci.IsColdCandidate,0) !=1) as cd                     
group by imid,candidateEmail,IRFId,candidateName,JDTiltle,MobileNumber,JobCountry, CandidateMasterId,ChannelOwner,CandidateId,InterviewId)as xy                       
join Interviewmeetings im on im.interviewdate=xy.dtCreatedOn and im.candidateEmail=xy.candidateEmail and im.IRFId=xy.IRFId                    
join InterviewComments ic on im.id=ic.interviewid                 
join IRF on im.IRFId=IRF.IRFId                
join Client cl on cl.id=IRF.ClientId    
outer apply(select top 1 csom.StatusValue From CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id  where cs.IRFId=xy.imid and CandidateId = xy.CandidateMasterId order by cs.Date desc) as CandidateStatus  
where MeetingStatus=1 or MeetingStatus=3 or MeetingStatus=5 or MeetingStatus=9 or confirmationStatus=2       
order by dtCreatedOn desc                         
                    
End                      
End                      
End   
    
  
GO