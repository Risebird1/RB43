  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientIRFCountDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetClientIRFCountDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetClientIRFCountDetails]                   
@EmailId varchar(200),  
@st datetime,  
@se datetime  
AS              
Begin             
          
declare @ClientId int  
  set @ClientId = (select ClientId from  ClientUser cu left join UserList ul on cu.UserId = ul.id  where ul.EmailId = @EmailId)  
    
DECLARE @tbl_RejectedMeetings TABLE (            
    IRFId INT,  
 ClientHrContect varchar(150),  
    RejectedMeetings int             
);            
            
DECLARE @tbl_TotalMeetings TABLE (            
    IRFId INT,          
 ClientHrContect varchar(150),  
    TotalMeetings int             
);            
            
DECLARE @tbl_SelectedMeetings TABLE (            
    IRFId INT,            
 ClientHrContect varchar(150),  
    SelectedMeetings int             
);            
      
DECLARE @tbl_CompletedMeetings TABLE (            
    IRFId INT,            
 ClientHrContect varchar(150),  
    ScheduledMeetings int             
);            
          
              
       
      
  INSERT INTO @tbl_RejectedMeetings            
  select irf.id as IRFId,ISNULL(im.ClientHrContact,'') as ClientHrContect, count(im.id) as RejectedMeetings            
from irf left join InterviewMeetings im on irf.IRFId= im.IRFId             
left join InterviewComments ic on im.id= ic.InterviewId            
inner join UserList ul on ul.EmailId = im.candidateEmail             
inner join CandidatesMaster cm on ul.id =cm.UserId            
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)            
where (ic.InterviewStatus= 1 or ic.InterviewStatus=3) and im.confirmationStatus=1 and irf.ClientId=@ClientId  
and im.interviewDate>=@st and im.interviewDate<=@se  
group by irf.id,im.ClientHrContact  
   
          
            
  INSERT INTO @tbl_SelectedMeetings            
select irf.id as IRFId,ISNULL(im.ClientHrContact,'') as ClientHrContect, count(im.id) as SelectedMeetings            
from irf left join InterviewMeetings im on irf.IRFId= im.IRFId             
left join InterviewComments ic on im.id= ic.InterviewId            
left join QCFeedbackReport qcf on qcf.InterviewId = im.id            
inner join UserList ul on ul.EmailId = im.candidateEmail             
inner join CandidatesMaster cm on ul.id =cm.UserId            
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)            
where (ic.InterviewStatus= 2 or ic.InterviewStatus=4)  and irf.IsQCRequired=1 and im.confirmationStatus=1 and irf.ClientId=@ClientId  
and im.interviewDate>=@st and im.interviewDate<=@se  
group by irf.id,im.ClientHrContact  
union      
select irf.id as IRFId,ISNULL(im.ClientHrContact,'') as ClientHrContect, count(im.id) as SelectedMeetings            
from irf left join InterviewMeetings im on irf.IRFId= im.IRFId             
left join InterviewComments ic on im.id= ic.InterviewId            
inner join UserList ul on ul.EmailId = im.candidateEmail             
inner join CandidatesMaster cm on ul.id =cm.UserId            
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)            
where (ic.InterviewStatus= 2 or ic.InterviewStatus=4) and ISNULL(irf.IsQCRequired,0)=0 and im.confirmationStatus=1 and irf.ClientId=@ClientId  
and im.interviewDate>=@st and im.interviewDate<=@se  
group by irf.id,im.ClientHrContact  
   
      
  
             
  INSERT INTO @tbl_CompletedMeetings            
  select irf.id as IRFId,ISNULL(im.ClientHrContact,'') as ClientHrContect, count(im.id) as ScheduledMeetings             
  from irf left join InterviewMeetings im on irf.IRFId = im.IRFId             
  left join InterviewComments ic on ic.InterviewId= im.id             
  inner join UserList ul on ul.EmailId = im.candidateEmail             
  inner join CandidatesMaster cm on ul.id =cm.UserId            
  inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)            
  where ic.MeetingStatus=4 and im.confirmationStatus=1 and irf.ClientId=@ClientId  
  and im.interviewDate>=@st and im.interviewDate<=@se  
  group by irf.id,im.ClientHrContact  
      
      
    
   select irf.id, irf.IRFId, irf.JDTiltle,            
   Client.[Name] as ClientCompName, tcm.RejectedMeetings,tsm.SelectedMeetings,          
   tschm.ScheduledMeetings,tschm.ClientHrContect as ClientHrContact,irf.IsPaused,cu.IsActive as IsRecruiterActive,cu.ClientId,cu.UserId  
   from irf left join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id left join [dbo].[UserList] ul on ul.id = tc.ClientUserId            
   left join ClientUser cu on tc.ClientUserId = cu.UserId            
   left join InterviewMeetings im on irf.IRFId = im.IRFId left join Client on IRF.ClientId = Client.id   
   left join @tbl_RejectedMeetings tcm on irf.id = tcm.IRFId and tcm.ClientHrContect=Isnull(im.ClientHrContact,'')           
   left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId and tsm.ClientHrContect=Isnull(im.ClientHrContact,'')           
   left join @tbl_CompletedMeetings tschm on irf.id= tschm.IRFId and tschm.ClientHrContect=Isnull(im.ClientHrContact,'')           
   left join UserList ulh on ulh.id = irf.RisebirdHr          
   where cu.ClientId=@ClientId  and (tcm.RejectedMeetings !=0 or tsm.SelectedMeetings!= 0 or tschm.ScheduledMeetings!=0)   
   and im.interviewDate>=@st and im.interviewDate<=@se  
   group by irf.id, irf.IRFId, irf.JDTiltle,            
   Client.[Name], tcm.RejectedMeetings, tsm.SelectedMeetings,          
   tschm.ScheduledMeetings, tschm.ClientHrContect,irf.IsPaused,cu.IsActive,cu.ClientId,cu.UserId       
    order by tschm.ClientHrContect         
 End
GO    
 

 