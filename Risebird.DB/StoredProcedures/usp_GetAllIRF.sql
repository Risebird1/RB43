

/****** Object:  StoredProcedure [dbo].[usp_GetAllIRF]    Script Date: 20-03-2020 12.16.55 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllIRF
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetAllIRF]           
@EmailId varchar(200),             
@Profile varchar(100),          
@timeNow datetime,  
@pageNumber int,  
@pageSize int,  
@searchValue varchar(100),  
@isFilterApplied bit,  
@startDate datetime = null,  
@endDate datetime = null,  
@IRFIdList varchar(max) = null,  
@ClientCompanyList varchar(max) = null,  
@EndCLientNameList varchar(max) = null,  
@PrimarySkillList varchar(max) = null  
AS            
Begin           
  
DECLARE @tbl_IRFSubset TABLE (          
    IRFId INT        
)   
  
DECLARE @tbl_FilterPrimarySkills TABLE (          
    PrimarySkill varchar(200)        
)   
  
if(@PrimarySkillList is not Null and @PrimarySkillList != '')  
Begin  
Insert into @tbl_FilterPrimarySkills  
select value from string_split(@PrimarySkillList, ',')  
End  
  
         
DECLARE @tbl_ConfirmedMeetings TABLE (          
    IRFId INT,          
    ConfirmedMeetings int           
);          
INSERT INTO @tbl_ConfirmedMeetings          
select IRF.id as IRFId, count(im.confirmationStatus) as ConfirmedMeetings           
from IRF left join InterviewMeetings im on IRF.IRFId = im.IRFId    
--join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
inner join UserList ul on ul.EmailId = im.candidateEmail           
inner join CandidatesMaster cm on ul.id =cm.UserId          
inner join Candidate ca on cm.id=ca.CandidateMasterId          
where im.confirmationStatus=1 group by IRF.id          
          
DECLARE @tbl_TotalMeetings TABLE (          
    IRFId INT,          
    TotalMeetings int           
);          
INSERT INTO @tbl_TotalMeetings          
  Select ca.IRFId, Count(ca.id) as TotalMeetings from Candidate ca     
--join @tbl_IRFSubset irfSub on irfSub.IRFId = ca.IRFId  
join CandidatesMaster cm on ca.CandidateMasterId=cm.id           
group by ca.IRFId        
          
DECLARE @tbl_SelectedMeetings TABLE (          
    IRFId INT,          
    SelectedMeetings int           
);          
INSERT INTO @tbl_SelectedMeetings          
select irf.id as IRFId, count(im.id) as SelectedMeetings          
from irf   
--join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
left join InterviewMeetings im on irf.IRFId= im.IRFId           
left join InterviewComments ic on im.id= ic.InterviewId          
left join QCFeedbackReport qcf on qcf.InterviewId = im.id          
inner join UserList ul on ul.EmailId = im.candidateEmail           
inner join CandidatesMaster cm on ul.id =cm.UserId          
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)          
where (ic.InterviewStatus= 2 or ic.InterviewStatus=4)  --and qcf.InterviewId is not null          
group by irf.id    
union    
select irf.id as IRFId, count(im.id) as SelectedMeetings          
from irf   
--join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
left join InterviewMeetings im on irf.IRFId= im.IRFId           
left join InterviewComments ic on im.id= ic.InterviewId          
inner join UserList ul on ul.EmailId = im.candidateEmail           
inner join CandidatesMaster cm on ul.id =cm.UserId          
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)          
where (ic.InterviewStatus= 2 or ic.InterviewStatus=4) and ISNULL(irf.IsQCRequired,0)=0    
group by irf.id    
    
DECLARE @tbl_CompletedMeetings TABLE (          
    IRFId INT,          
    ScheduledMeetings int           
);          
INSERT INTO @tbl_CompletedMeetings          
select irf.id as IRFId, count(im.id) as ScheduledMeetings           
from irf   
--join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
left join InterviewMeetings im on irf.IRFId = im.IRFId           
left join InterviewComments ic on ic.InterviewId= im.id           
inner join UserList ul on ul.EmailId = im.candidateEmail           
inner join CandidatesMaster cm on ul.id =cm.UserId          
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)          
where ic.MeetingStatus in (4,6,7,8)    and im.confirmationStatus=1         
group by irf.id        
        
          
          
DECLARE @tbl_SubscribedTo TABLE (          
    SubId int,          
    IRFId INT          
);          
INSERT INTO @tbl_SubscribedTo          
select id, Subscribers.IRFId from Subscribers     
--join @tbl_IRFSubset irfSub on Subscribers.IRFId = irfSub.IRFId  
where UserId= (select id from  UserList ul where ul.EmailId = @EmailId and idProfile= (select id from [Profile] where [Name]=@Profile))          
   
          
IF (@Profile = 'Client')            
 BEGIN            
  declare @roleId int,  @ClientId int     
  set @roleId = (select RoleId from  ClientUser cu  join UserList ul on cu.UserId = ul.id  where ul.EmailId = @EmailId)    
  set @ClientId = (select ClientId from  ClientUser cu left join UserList ul on cu.UserId = ul.id  where ul.EmailId = @EmailId)          
     
  if(@roleId=1)    
  begin    
  delete from @tbl_ConfirmedMeetings    
    
  INSERT INTO @tbl_ConfirmedMeetings          
  select IRF.id as IRFId, count(im.confirmationStatus) as ConfirmedMeetings           
  from IRF left join InterviewMeetings im on IRF.IRFId = im.IRFId           
  inner join UserList ul on ul.EmailId = im.candidateEmail     
  inner join CandidatesMaster cm on ul.id =cm.UserId          
  inner join Candidate ca on cm.id=ca.CandidateMasterId          
  where im.confirmationStatus=1 and ca.ClientHRAttemptContact=@EmailId group by IRF.id          
          
  delete from @tbl_TotalMeetings    
    
  INSERT INTO @tbl_TotalMeetings          
    Select ca.IRFId, Count(ca.id) as TotalMeetings from Candidate ca          
  join CandidatesMaster cm on ca.CandidateMasterId=cm.id    
  and ca.ClientHRAttemptContact=@EmailId    
  group by ca.IRFId        
          
  delete from @tbl_SelectedMeetings    
    
  INSERT INTO @tbl_SelectedMeetings          
  select irf.id as IRFId, count(im.id) as SelectedMeetings          
  from irf left join InterviewMeetings im on irf.IRFId= im.IRFId           
  left join InterviewComments ic on im.id= ic.InterviewId          
  left join QCFeedbackReport qcf on qcf.InterviewId = im.id          
  inner join UserList ul on ul.EmailId = im.candidateEmail           
  inner join CandidatesMaster cm on ul.id =cm.UserId          
  inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)          
  where (ic.InterviewStatus= 2 or ic.InterviewStatus=4) and qcf.InterviewId is not null     
  and ca.ClientHRAttemptContact=@EmailId    
  group by irf.id          
          
       delete from @tbl_CompletedMeetings    
    
  INSERT INTO @tbl_CompletedMeetings          
  select irf.id as IRFId, count(im.id) as ScheduledMeetings           
  from irf left join InterviewMeetings im on irf.IRFId = im.IRFId           
  left join InterviewComments ic on ic.InterviewId= im.id           
  inner join UserList ul on ul.EmailId = im.candidateEmail           
  inner join CandidatesMaster cm on ul.id =cm.UserId          
  inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)          
  where ic.MeetingStatus=4 and im.confirmationStatus=1 and ca.ClientHRAttemptContact=@EmailId       
  group by irf.id     
    
 delete from @tbl_IRFSubset  
    
 INSERT INTO @tbl_IRFSubset   
 select modifiedIRF.id from ( select ROW_NUMBER() over (order by irf.id desc) as rowNum,  
 irf.id from IRF   
 join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id   
 left join [dbo].[UserList] ul on ul.id = tc.ClientUserId   
 left join ClientUser cu on tc.ClientUserId = cu.UserId   
 left join Client cl on cl.id = irf.ClientId  
 where ul.EmailId=@EmailId and cu.ClientId=@ClientId   
 and (((cast(irf.EndDate as date) > cast(@timeNow as date)) and @isFilterApplied=0) or @isFilterApplied=1)  
 and (irf.IRFId like '%' + @searchValue +'%'  
 or @searchValue IS NULL or @searchValue = '')  
 and (cast(irf.StartDate as date) >= cast(@startDate as date)  
 or @startDate IS NULL)  
 and (cast(irf.EndDate as date) <= cast(@endDate as date)  
 or @endDate IS NULL)  
 and (irf.IRFId in (select value from string_split(@IRFIdList, ','))  
 or @IRFIdList IS NULL or @IRFIdList = '')  
 and (cl.[Name] in (select value from string_split(@ClientCompanyList, ','))  
 or @ClientCompanyList IS NULL or @ClientCompanyList = '')  
 and (irf.ClientName in (select value from string_split(@EndCLientNameList, ','))  
 or @EndCLientNameList IS NULL or @EndCLientNameList = '')  
 and ((charindex('|', irf.JDTiltle) != 0 and TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1)) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or (charindex('|', irf.JDTiltle) = 0 and Trim(irf.JDTiltle) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or @PrimarySkillList IS NULL or @PrimarySkillList = '')) modifiedIRF where modifiedIRF.rowNum <= (@pageNumber*@pageSize) and  modifiedIRF.rowNum >=((@pageNumber-1)*@pageSize + 1)  
      
    
   select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,          
   irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails, irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel, irf.IsPaused, irf.ClientName,          
   Client.[Name] as ClientCompName, tcm.ConfirmedMeetings, ttm.TotalMeetings, tsm.SelectedMeetings,        
   tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings) as TotalBudgetConsumed, 1 as IsClientUser, ulh.EmailId as RisebirdHREmail,  
   irf.flagcount, irf.CreatedFromTemplateId          
   from irf   
   join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id
   join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id   
   left join [dbo].[UserList] ul on ul.id = tc.ClientUserId          
   left join ClientUser cu on tc.ClientUserId = cu.UserId          
   --left join InterviewMeetings im on irf.IRFId = im.IRFId   
   left join Client on IRF.ClientId = Client.id   
   left join @tbl_ConfirmedMeetings tcm on irf.id = tcm.IRFId          
   left join @tbl_TotalMeetings ttm on irf.id=ttm.IRFId          
   left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId          
   left join @tbl_CompletedMeetings tschm on irf.id= tschm.IRFId         
   left join UserList ulh on ulh.id = irf.RisebirdHr        
   where ul.EmailId = @EmailId and cu.ClientId=@ClientId          
   
  end    
  else    
  begin   
  
 delete from @tbl_IRFSubset  
    
 INSERT INTO @tbl_IRFSubset   
 select modifiedIRF.id from ( select ROW_NUMBER() over (order by irf.id desc) as rowNum,  
 irf.id from IRF   
 left join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id   
 left join [dbo].[UserList] ul on ul.id = tc.ClientUserId   
 left join ClientUser cu on tc.ClientUserId = cu.UserId   
 left join Client cl on cl.id = irf.ClientId  
 where ul.EmailId=@EmailId and cu.ClientId=@ClientId  
 and (((cast(irf.EndDate as date) > cast(@timeNow as date)) and @isFilterApplied=0) or @isFilterApplied=1)  
 and (irf.IRFId like '%' + @searchValue +'%'  
 or @searchValue IS NULL or @searchValue = '')  
 and (cast(irf.StartDate as date) >= cast(@startDate as date)  
 or @startDate IS NULL)  
 and (cast(irf.EndDate as date) <= cast(@endDate as date)  
 or @endDate IS NULL)  
 and (irf.IRFId in (select value from string_split(@IRFIdList, ','))  
 or @IRFIdList IS NULL or @IRFIdList = '')  
 and (cl.[Name] in (select value from string_split(@ClientCompanyList, ','))  
 or @ClientCompanyList IS NULL or @ClientCompanyList = '')  
 and (irf.ClientName in (select value from string_split(@EndCLientNameList, ','))  
 or @EndCLientNameList IS NULL or @EndCLientNameList = '')  
 and ((charindex('|', irf.JDTiltle) != 0 and TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1)) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or (charindex('|', irf.JDTiltle) = 0 and Trim(irf.JDTiltle) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or @PrimarySkillList IS NULL or @PrimarySkillList = '')) modifiedIRF where modifiedIRF.rowNum <= (@pageNumber*@pageSize) and  modifiedIRF.rowNum >=((@pageNumber-1)*@pageSize + 1)  
      
  
   select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,          
   irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails, irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel, irf.IsPaused, irf.ClientName,          
   Client.[Name] as ClientCompName, tcm.ConfirmedMeetings, ttm.TotalMeetings, tsm.SelectedMeetings,        
   tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings) as TotalBudgetConsumed, 1 as IsClientUser,  
   ulh.EmailId as RisebirdHREmail,irf.flagcount, irf.CreatedFromTemplateId            
   from irf   
   join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
   left join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id   
   left join [dbo].[UserList] ul on ul.id = tc.ClientUserId          
   left join ClientUser cu on tc.ClientUserId = cu.UserId          
   --left join InterviewMeetings im on irf.IRFId = im.IRFId   
   left join Client on IRF.ClientId = Client.id   
   left join @tbl_ConfirmedMeetings tcm on irf.id = tcm.IRFId          
   left join @tbl_TotalMeetings ttm on irf.id=ttm.IRFId          
   left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId          
   left join @tbl_CompletedMeetings tschm on irf.id= tschm.IRFId         
   left join UserList ulh on ulh.id = irf.RisebirdHr        
   where ul.EmailId = @EmailId and cu.ClientId=@ClientId          
        
  end    
           
 End            
Else if(@Profile='Recruiter')          
 Begin       
   
 delete from @tbl_IRFSubset  
    
 INSERT INTO @tbl_IRFSubset   
 select modifiedIRF.id from ( select ROW_NUMBER() over (order by irf.id desc) as rowNum,  
 irf.id from IRF   
 left join [dbo].[TransRecruiter] tr on tr.IRFId = IRF.id   
    left join [dbo].[UserList] ul on ul.id = tr.UserId   
 left join Client cl on cl.id = irf.ClientId  
 where ul.EmailId=@EmailId  
 and (((cast(irf.EndDate as date) > cast(@timeNow as date)) and @isFilterApplied=0) or @isFilterApplied=1)  
 and (irf.IRFId like '%' + @searchValue +'%'  
 or @searchValue IS NULL or @searchValue = '')  
 and (cast(irf.StartDate as date) >= cast(@startDate as date)  
 or @startDate IS NULL)  
 and (cast(irf.EndDate as date) <= cast(@endDate as date)  
 or @endDate IS NULL)  
 and (irf.IRFId in (select value from string_split(@IRFIdList, ','))  
 or @IRFIdList IS NULL or @IRFIdList = '')  
 and (cl.[Name] in (select value from string_split(@ClientCompanyList, ','))  
 or @ClientCompanyList IS NULL or @ClientCompanyList = '')  
 and (irf.ClientName in (select value from string_split(@EndCLientNameList, ','))  
 or @EndCLientNameList IS NULL or @EndCLientNameList = '')  
 and ((charindex('|', irf.JDTiltle) != 0 and TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1)) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or (charindex('|', irf.JDTiltle) = 0 and Trim(irf.JDTiltle) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or @PrimarySkillList IS NULL or @PrimarySkillList = '')) modifiedIRF where modifiedIRF.rowNum <= (@pageNumber*@pageSize) and  modifiedIRF.rowNum >=((@pageNumber-1)*@pageSize + 1)  
      
  select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,          
  irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails,irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel, irf.IsPaused, irf.ClientName,          
  Client.[Name] as ClientCompName, tcm.ConfirmedMeetings, ttm.TotalMeetings, tsm.SelectedMeetings,        
  tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings) as TotalBudgetConsumed, ulh.EmailId as RisebirdHREmail,  
  irf.flagcount, irf.CreatedFromTemplateId            
  from irf   
  join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
  left join [dbo].[TransRecruiter] tr on tr.IRFId = IRF.id   
  left join [dbo].[UserList] ul on ul.id = tr.UserId          
  --left join InterviewMeetings im on irf.IRFId = im.IRFId   
  left join Client on IRF.ClientId = Client.id   
  left join @tbl_ConfirmedMeetings tcm on irf.id = tcm.IRFId          
  left join @tbl_TotalMeetings ttm on irf.id=ttm.IRFId          
  left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId          
  left join @tbl_CompletedMeetings tschm on irf.id= tschm.IRFId          
  left join UserList ulh on ulh.id = irf.RisebirdHr        
  where ul.EmailId = @EmailId           
           
 End          
Else if(@Profile='CuratedUser')          
 Begin    
 delete from @tbl_IRFSubset  
  
 INSERT INTO @tbl_IRFSubset   
 select modifiedIRF.id from ( select ROW_NUMBER() over (order by irf.id desc) as rowNum,  
 irf.id from IRF   
 left join Client cl on IRF.ClientId = cl.id  
 left join UserList ulh on ulh.id = irf.RisebirdHr        
 where cl.CuratedLink=1  
 and (((cast(irf.EndDate as date) > cast(@timeNow as date)) and @isFilterApplied=0) or @isFilterApplied=1)  
 and (irf.IRFId like '%' + @searchValue +'%'  
 or @searchValue IS NULL or @searchValue = '')  
 and (cast(irf.StartDate as date) >= cast(@startDate as date)  
 or @startDate IS NULL)  
 and (cast(irf.EndDate as date) <= cast(@endDate as date)  
 or @endDate IS NULL)  
 and (irf.IRFId in (select value from string_split(@IRFIdList, ','))  
 or @IRFIdList IS NULL or @IRFIdList = '')  
 and (cl.[Name] in (select value from string_split(@ClientCompanyList, ','))  
 or @ClientCompanyList IS NULL or @ClientCompanyList = '')  
 and (irf.ClientName in (select value from string_split(@EndCLientNameList, ','))  
 or @EndCLientNameList IS NULL or @EndCLientNameList = '')  
 and ((charindex('|', irf.JDTiltle) != 0 and TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1)) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or (charindex('|', irf.JDTiltle) = 0 and Trim(irf.JDTiltle) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or @PrimarySkillList IS NULL or @PrimarySkillList = '')) modifiedIRF where modifiedIRF.rowNum <= (@pageNumber*@pageSize) and  modifiedIRF.rowNum >=((@pageNumber-1)*@pageSize + 1)  
      
          
  select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.IsDeleted,          
  irf.CalendlyLink, irf.IsPaused, irf.ClientName,          
  Client.[Name] as ClientCompName, tsm.SelectedMeetings, sub.SubId, ulh.EmailId as RisebirdHREmail,  
  irf.flagcount, irf.CreatedFromTemplateId            
  from irf   
  join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
  left join Client on IRF.ClientId = Client.id          
  left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId          
  left join @tbl_SubscribedTo sub on irf.id=sub.IRFId         
  left join UserList ulh on ulh.id = irf.RisebirdHr        
  where Client.CuratedLink=1           
  
 End         
Else if(@Profile='HR')          
 Begin   
  
 delete from @tbl_IRFSubset  
   
 select modifiedIRF.id from (select ROW_NUMBER() over (order by filteredIRF.id desc) as rowNum, filteredIRF.id from (select irf.id from IRF   
 inner join Client on IRF.ClientId = Client.id  
 join TransHR th on th.IRFId = irf.id        
 join HR on HR.id = th.HRId        
 join UserList ulh on ulh.id = HR.UserId         
 left join Userlist ul on ul.id=irf.RisebirdHr   
 left join CLient cl on cl.id = IRF.ClientId  
 where ulh.EmailId=@EmailId  
 and (((cast(irf.EndDate as date) > cast(@timeNow as date)) and @isFilterApplied=0) or @isFilterApplied=1)  
 and (irf.IRFId like '%' + @searchValue +'%'  
 or @searchValue IS NULL or @searchValue = '')  
 and (cast(irf.StartDate as date) >= cast(@startDate as date)  
 or @startDate IS NULL)  
 and (cast(irf.EndDate as date) <= cast(@endDate as date)  
 or @endDate IS NULL)  
 and (irf.IRFId in (select value from string_split(@IRFIdList, ','))  
 or @IRFIdList IS NULL or @IRFIdList = '')  
 and (cl.[Name] in (select value from string_split(@ClientCompanyList, ','))  
 or @ClientCompanyList IS NULL or @ClientCompanyList = '')  
 and (irf.ClientName in (select value from string_split(@EndCLientNameList, ','))  
 or @EndCLientNameList IS NULL or @EndCLientNameList = '')  
 and ((charindex('|', irf.JDTiltle) != 0 and TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1)) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or (charindex('|', irf.JDTiltle) = 0 and Trim(irf.JDTiltle) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or @PrimarySkillList IS NULL or @PrimarySkillList = '')  
 union  
 select irf.id from IRF   
 join TransHR th on th.IRFId = irf.id        
 join HR on HR.id = th.HRId        
 join UserList ulh on ulh.id = HR.UserId         
 left join Userlist ul on ul.id=irf.RisebirdHr     
 left join Client cl on cl.id = irf.ClientId  
 where ulh.EmailId=@EmailId   
 and (((cast(irf.EndDate as date) > cast(@timeNow as date)) and @isFilterApplied=0) or @isFilterApplied=1)  
 and (irf.IRFId like '%' + @searchValue +'%'  
 or @searchValue IS NULL or @searchValue = '')  
 and (cast(irf.StartDate as date) >= cast(@startDate as date)  
 or @startDate IS NULL)  
 and (cast(irf.EndDate as date) <= cast(@endDate as date)  
 or @endDate IS NULL)  
 and (irf.IRFId in (select value from string_split(@IRFIdList, ','))  
 or @IRFIdList IS NULL or @IRFIdList = '')  
 and (cl.[Name] in (select value from string_split(@ClientCompanyList, ','))  
 or @ClientCompanyList IS NULL or @ClientCompanyList = '')  
 and (irf.ClientName in (select value from string_split(@EndCLientNameList, ','))  
 or @EndCLientNameList IS NULL or @EndCLientNameList = '')  
 and ((charindex('|', irf.JDTiltle) != 0 and TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1)) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or (charindex('|', irf.JDTiltle) = 0 and Trim(irf.JDTiltle) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or @PrimarySkillList IS NULL or @PrimarySkillList = '')) filteredIRF) modifiedIRF   
 where modifiedIRF.rowNum <= (@pageNumber*@pageSize)   
 and  modifiedIRF.rowNum >=((@pageNumber-1)*@pageSize + 1)  
  
  select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,          
  irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails, irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel,Client.CuratedLink as CuratedLink, irf.IsPaused, irf.ClientName,          
  Client.[Name] as ClientCompName, tcm.ConfirmedMeetings, ttm.TotalMeetings,        
  tsm.SelectedMeetings, tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings) as TotalBudgetConsumed,  
  ul.EmailId as RisebirdHREmail,irf.flagcount, irf.CreatedFromTemplateId            
  from irf   
  join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
  --left join InterviewMeetings im on irf.IRFId = im.IRFId   
  inner join Client on IRF.ClientId = Client.id   
  left join @tbl_ConfirmedMeetings tcm on irf.id = tcm.IRFId          
  left join @tbl_TotalMeetings ttm on irf.id=ttm.IRFId          
  left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId          
  left join @tbl_CompletedMeetings tschm on irf.id= tschm.IRFId          
  join TransHR th on th.IRFId = irf.id        
  join HR on HR.id = th.HRId        
  join UserList ulh on ulh.id = HR.UserId         
  left join Userlist ul on ul.id=irf.RisebirdHr      
  where ulh.EmailId=@EmailId        
  
  union      
  select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,          
  irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails, irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel,Client.CuratedLink as CuratedLink, irf.IsPaused, irf.ClientName,          
  Client.[Name] as ClientCompName, tcm.ConfirmedMeetings, ttm.TotalMeetings,        
  tsm.SelectedMeetings, tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings) as TotalBudgetConsumed,  
  ul.EmailId as RisebirdHREmail,irf.flagcount, irf.CreatedFromTemplateId           
  from irf   
  join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
  --left join InterviewMeetings im on irf.IRFId = im.IRFId   
  inner join Client on IRF.ClientId = Client.id   
  left join @tbl_ConfirmedMeetings tcm on irf.id = tcm.IRFId          
  left join @tbl_TotalMeetings ttm on irf.id=ttm.IRFId          
  left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId          
  left join @tbl_CompletedMeetings tschm on irf.id= tschm.IRFId          
  left join Userlist ul on ul.id=irf.RisebirdHr      
  where ul.EmailId=@EmailId        
       
 End    
Else if(@Profile='SeniorHR' or @Profile='SupportAdmin')          
 Begin          
       
 delete from @tbl_IRFSubset  
  
 INSERT INTO @tbl_IRFSubset   
 select modifiedIRF.id from ( select ROW_NUMBER() over (order by irf.id desc) as rowNum,  
 irf.id from IRF   
 inner join Client on IRF.ClientId = Client.id  
 join SeniorHR sh on sh.Clientid=Client.id    
 left join Userlist ul on ul.id=sh.Userid       
 where ul.EmailId=@EmailId  
 and (((cast(irf.EndDate as date) > cast(@timeNow as date)) and @isFilterApplied=0) or @isFilterApplied=1)  
 and (irf.IRFId like '%' + @searchValue +'%'  
 or @searchValue IS NULL or @searchValue = '')  
 and (cast(irf.StartDate as date) >= cast(@startDate as date)  
 or @startDate IS NULL)  
 and (cast(irf.EndDate as date) <= cast(@endDate as date)  
 or @endDate IS NULL)  
 and (irf.IRFId in (select value from string_split(@IRFIdList, ','))  
 or @IRFIdList IS NULL or @IRFIdList = '')  
 and (Client.[Name] in (select value from string_split(@ClientCompanyList, ','))  
 or @ClientCompanyList IS NULL or @ClientCompanyList = '')  
 and (irf.ClientName in (select value from string_split(@EndCLientNameList, ','))  
 or @EndCLientNameList IS NULL or @EndCLientNameList = '')  
 and ((charindex('|', irf.JDTiltle) != 0 and TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1)) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or (charindex('|', irf.JDTiltle) = 0 and Trim(irf.JDTiltle) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or @PrimarySkillList IS NULL or @PrimarySkillList = '')) modifiedIRF   
 where modifiedIRF.rowNum <= (@pageNumber*@pageSize) and  modifiedIRF.rowNum >=((@pageNumber-1)*@pageSize + 1)  
      
      
  select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,          
  irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails, irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel,Client.CuratedLink as CuratedLink, irf.IsPaused, irf.ClientName,          
  Client.[Name] as ClientCompName, tcm.ConfirmedMeetings, ttm.TotalMeetings,        
  tsm.SelectedMeetings, tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings) as TotalBudgetConsumed,  
  ulh.EmailId as RisebirdHREmail,irf.flagcount, irf.CreatedFromTemplateId          
  from irf   
  join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
  --left join InterviewMeetings im on irf.IRFId = im.IRFId     
  inner join Client on IRF.ClientId = Client.id     
  join SeniorHR sh on sh.Clientid=Client.id    
  left join Userlist ul on ul.id=sh.Userid    
  left join @tbl_ConfirmedMeetings tcm on irf.id = tcm.IRFId          
  left join @tbl_TotalMeetings ttm on irf.id=ttm.IRFId          
  left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId          
  left join @tbl_CompletedMeetings tschm on irf.id= tschm.IRFId          
  left join UserList ulh on ulh.id = irf.RisebirdHr        
  where ul.EmailId=@EmailId        
       
 End    
     
Else          
begin  
  
 delete from @tbl_IRFSubset  
  
 INSERT INTO @tbl_IRFSubset   
 select modifiedIRF.id from ( select ROW_NUMBER() over (order by irf.id desc) as rowNum,  
 irf.id from IRF   
 left join Client cl on cl.id = irf.ClientId  
 where (irf.IRFId like '%' + @searchValue +'%'  
 or @searchValue IS NULL or @searchValue = '')  
 and (((cast(irf.EndDate as date) > cast(@timeNow as date)) and @isFilterApplied=0) or @isFilterApplied=1)  
 and (cast(irf.StartDate as date) >= cast(@startDate as date)  
 or @startDate IS NULL)  
 and (cast(irf.EndDate as date) <= cast(@endDate as date)  
 or @endDate IS NULL)  
 and (irf.IRFId in (select value from string_split(@IRFIdList, ','))  
 or @IRFIdList IS NULL or @IRFIdList = '')  
 and (cl.[Name] in (select value from string_split(@ClientCompanyList, ','))  
 or @ClientCompanyList IS NULL or @ClientCompanyList = '')  
 and (irf.ClientName in (select value from string_split(@EndCLientNameList, ','))  
 or @EndCLientNameList IS NULL or @EndCLientNameList = '')  
 and ((charindex('|', irf.JDTiltle) != 0 and TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1)) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or (charindex('|', irf.JDTiltle) = 0 and Trim(irf.JDTiltle) in (select PrimarySkill from @tbl_FilterPrimarySkills))  
 or @PrimarySkillList IS NULL or @PrimarySkillList = '')) modifiedIRF   
 where modifiedIRF.rowNum <= (@pageNumber*@pageSize) and  modifiedIRF.rowNum >=((@pageNumber-1)*@pageSize + 1)  
      
 select ROW_NUMBER() over (order by irf.id desc) as rowNum,  
 irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,          
 irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails, irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel,Client.CuratedLink as CuratedLink, irf.IsPaused, irf.ClientName,          
 Client.[Name] as ClientCompName, tcm.ConfirmedMeetings, ttm.TotalMeetings,        
 tsm.SelectedMeetings, tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings) as TotalBudgetConsumed,  
 ulh.EmailId as RisebirdHREmail,irf.flagcount, irf.CreatedFromTemplateId          
 from irf   
 join @tbl_IRFSubset irfSub on irfSub.IRFId = irf.id  
 --left join InterviewMeetings im on irf.IRFId = im.IRFId   
 inner join Client on IRF.ClientId = Client.id   
 left join @tbl_ConfirmedMeetings tcm on irf.id = tcm.IRFId          
 left join @tbl_TotalMeetings ttm on irf.id=ttm.IRFId          
 left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId          
 left join @tbl_CompletedMeetings tschm on irf.id= tschm.IRFId          
 left join UserList ulh on ulh.id = irf.RisebirdHr        
     
End          
End    
GO


