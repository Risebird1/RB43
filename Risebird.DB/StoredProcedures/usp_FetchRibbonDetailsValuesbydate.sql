IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchRibbonDetailsValuesbydate'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchRibbonDetailsValuesbydate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create procedure usp_FetchRibbonDetailsValuesbydate    
@actionstatus varchar(30),    
@actiontype int,    
@emailid varchar(200),    
@currentdate datetime,  
@StartDate datetime,  
@EndDate datetime  
AS    
BEGIN    
Declare @Profile varchar(150)     
Select @Profile=pr.Name from UserList ul join Profile pr on pr.id=ul.idProfile where EmailId=@emailid    
 If(@actionstatus ='InterviewCount')    
 begin    
  if (@actiontype = 0)    
  begin    
   if (@Profile = 'SeniorHR' or @Profile = 'HR')    
   begin    
  select distinct im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,cl.Name,   
    IRF.CalendlyLink,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,  
    LEN(IRF.CalendlyLink)) as scheduleUniqueid, im.id as imid, irf.AssessmentGroupId, asg.AssessmentGroup from InterviewMeetings im   
    join IRF on im.IRFId=irf.IRFId   
    join Client cl on irf.ClientId=cl.id   
    join SeniorHr sh on sh.Clientid=irf.ClientId   
    left join UserList ulsh on ulsh.id=sh.Userid   
    join UserList ul on ul.id= irf.RisebirdHr  
    join AssessmentGroup asg on asg.id = irf.AssessmentGroupId  
    where confirmationStatus=1 and interviewer is null and ul.Emailid= @Emailid and interviewDate between @StartDate and @EndDate  
   end    
   else    
   begin    
    select distinct im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,cl.Name,IRF.CalendlyLink,  
    SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,  
    LEN(IRF.CalendlyLink)) as scheduleUniqueid, im.id as imid, irf.AssessmentGroupId, asg.AssessmentGroup   
    from InterviewMeetings im   
    join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
    join UserList ul on ul.id=  irf.RisebirdHr  
    join AssessmentGroup asg on asg.id = irf.AssessmentGroupId  
    where confirmationStatus=1 and interviewer is null and interviewDate between @StartDate and @EndDate  
   end    
  end    
  else    
  begin    
   select distinct im.*,irf.*,cl.*, ulc.EmailId as ChannelOwner,IRF.Id as irfIntId ,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,cl.Name,  
   IRF.CalendlyLink,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,   
   LEN(IRF.CalendlyLink)) as scheduleUniqueid,im.id as imid, asg.AssessmentGroup  
   from InterviewMeetings im join IRF on im.IRFId=irf.IRFId   
   join Client cl on irf.ClientId=cl.id   
   join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId   
   join UserList ulc on ulc.id=  irf.RisebirdHr  
   join AssessmentGroup asg on asg.id = irf.AssessmentGroupId  
   where confirmationStatus=1 and interviewer is null and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate    
   union     
   select distinct im.*,irf.*,cl.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,cl.Name,   
   IRF.CalendlyLink, SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,  
   LEN(IRF.CalendlyLink)) as scheduleUniqueid,im.id as imid, asg.AssessmentGroup  from InterviewMeetings im  
   join IRF on im.IRFId=irf.IRFId   
   join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr   
   join UserList ulc on ulc.id=  irf.RisebirdHr  
   join AssessmentGroup asg on asg.id = irf.AssessmentGroupId  
   where confirmationStatus=1 and interviewer is null and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
  end     end    
 else If(@actionstatus ='MeetingStatus')    
begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
    select distinct im.*,irf.*,cl.*,ic.*,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
    join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid   
    left join InterviewComments ic on ic.InterviewId= im.id   
    left join UserList uli on uli.EmailId=im.interviewer  
    left join UserList ulc on ulc.id=IRF.RisebirdHr  
    where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewDate between @StartDate and @EndDate and ulc.Emailid= @Emailid and interviewdate<@currentdate and ISNULL(IRF.IsAIInterview,0)=0  
   end  
   else  
   begin  
    select distinct im.*,irf.*,cl.*,ic.*,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
    left join InterviewComments ic on ic.InterviewId= im.id   
    left join UserList uli on uli.EmailId=im.interviewer  
    left join UserList ulc on ulc.id=IRF.RisebirdHr  
    where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewDate between @StartDate and @EndDate and interviewdate<@currentdate and ISNULL(IRF.IsAIInterview,0)=0  
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.*,ic.*,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   left join InterviewComments ic on ic.InterviewId= im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId   
   left join UserList uli on uli.EmailId=im.interviewer left join UserList ulc on ulc.id=IRF.RisebirdHr  
   where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewDate between @StartDate and @EndDate and ul.EmailId=@emailID and interviewdate<@currentdate and ISNULL(IRF.IsAIInterview,0)=0  
   union   
   select distinct im.*,irf.*,cl.*,ic.*,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   left join InterviewComments ic on ic.InterviewId= im.id join Userlist ul on ul.id=irf.RisebirdHr   
   left join UserList uli on uli.EmailId=im.interviewer left join UserList ulc on ulc.id=IRF.RisebirdHr  
   where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewDate between @StartDate and @EndDate and ul.Emailid =@emailID and interviewdate<@currentdate and ISNULL(IRF.IsAIInterview,0)=0  
  end  
 end  
 else If(@actionstatus ='UnAssignedCoordianter')    
 begin    
  if (@Profile = 'SeniorHR' or @Profile = 'HR')    
  begin    
   select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id     
   join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id     
   where confirmationStatus=1 and isAutoCancelled=1 and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate    
  end    
  else    
  begin    
   select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id     
   left join InterviewComments ic on ic.InterviewId=im.id     
   where confirmationStatus=1 and isAutoCancelled=1 and interviewDate between @StartDate and @EndDate  
  end    
 end    
 else If(@actionstatus ='ZoomLink')    
 begin    
  if (@actiontype = 0)    
  begin    
   if (@Profile = 'SeniorHR' or @Profile = 'HR')    
   begin    
select  distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.
id where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate  
   end    
   else    
   begin    
    select  distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate 
between @StartDate and @EndDate  
   end    
  end    
  else    
  begin    
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join
 Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
   union     
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=
1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
  end    
 end    
 else If(@actionstatus ='UnAttendedZoomLink')    
 begin    
  if (@actiontype = 0)    
  begin    
   if (@Profile = 'SeniorHR' or @Profile = 'HR')    
   begin    
 Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.WhoJoined from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on 
irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer join TransZoomMeetingExtendDetails WhoJoined on WhoJoined.Meetingid=im.ZoomMeetingId and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate  
   end    
   else    
   begin    
    Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.WhoJoined from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl 
on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer join TransZoomMeetingExtendDetails WhoJoined on WhoJoined.Meetingid=im.ZoomMeetingId and interviewDate between @StartDate and @EndDate  
   end    
  end    
  else    
  begin    
   select distinct im.*,irf.*,cl.*,ic.*,uli.Emailid as HRCoordinatorEmailId,ulin.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist uli on uli.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist ulin on ulin.EmailId=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined where confirmationStatus=1 and uli.EmailId =@emailID and im.ZoomMeetingId 
in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid) and interviewDate between @StartDate and @EndDate  
  end    
 end    
 else If(@actionstatus ='VideoLink')    
 begin    
  if (@actiontype = 0)    
  begin    
   if (@Profile = 'SeniorHR' or @Profile = 'HR')    
   begin    
    Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=
im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate and Isnull(IRF.IsAIInterview,0)=0  
   end    
   else    
   begin    
    Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate between @StartDate and @EndDate and Isnull(IRF.IsAIInterview,0)=0  
   end    
  end    
  else    
  begin    
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join
 Userlist ul on ul.id=HR.UserId where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate and Isnull(IRF.IsAIInterview,0)=0  
   union     
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where ic.MeetingStatus=4 
and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate and Isnull(IRF.IsAIInterview,0)=0  
  end    
 end    
 else If(@actionstatus ='QCPendingReport')    
 begin    
  if (@actiontype = 0)    
  begin    
   if (@Profile = 'SeniorHR')    
   begin    
 Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,ic.AIVerdict,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,  
 cl.Name,uli.MobileNumber as InterviewerMobileNumber,ulco.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id  
 join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id   
 join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id   
 join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id  
 left join UserList ulco on IRF.RisebirdHr=ulco.id  
 where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ulco.EmailId= @Emailid  and interviewDate between @StartDate and @EndDate and ISNULL(IRF.IsAIInterview,0)=0  
   end    
   else    
   begin   
       Select distinct im.IRFId,im.ClientHrContact,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,im.SessionId,IRF.IsAIInterview,CASE  WHEN DATEDIFF(MINUTE, im.interviewDate,imjf.FirstJoinedon) >= 5 THEN 1  ELSE 0  END AS InterviewerLateJoin,IRF.IsVideoRequired,uli.MobileNumber as InterviewerMobileNumber,imjt.Joinedon,imjf.FirstJoinedon,ulco.EmailId as ChannelOwner,pm.DurationSelectedMin as interviewDuration,ic.AITranscriptVerdict,ic.AudioTranscriptFileLink,ic.AITranscriptAnalysisScore,ic.AIVerdict,cfd.Rating,Isnull(ulim.FirstName,'') as InterviewerName,(pm.DurationSelectedMin-ic.VideoDuration) as deficit,qia.ReportAI,qia.TranscritAi,
    Case when IsNull(qia.ContradictionJson,'')='' then 0 else 1 end ContradictionAi,ic.AudioTranscriptFileLink,ic.TranscriptSummary,IRF.PriorityLevel,d.NonAIreasoning,d.Riskcategory
    from InterviewMeetings im join IRF on im.IRFId=irf.IRFId
    left join QcInterviewAIevaluation qia on qia.InterviewId=im.id
    join Client cl on irf.ClientId=cl.id 
    join InterviewComments ic on ic.InterviewId=im.id 
    join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 
    join UserList ulim on ulim.EmailId=im.interviewer
    left join UserList ulco on IRF.RisebirdHr=ulco.id
    left join CandidateFeedbackDetails cfd on cfd.Interviewid=im.id 
    left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.CompanyType=IRF.CompanyType and pm.PlanTypeEnumValue=IRF.Heuristicsscore
    Outer Apply(Select Max(Joinedon) Joinedon from  InterviewMeetingsJoinTime 
    left join UserList ulco on IRF.RisebirdHr=ulco.id where im.interviewer=interviewer and im.id=interviewid) imjt 
    Outer Apply(Select min(Joinedon) FirstJoinedon from  InterviewMeetingsJoinTime where im.id=interviewid) imjf  
    LEFT JOIN (SELECT InterviewId,STRING_AGG(Type, ',') AS NonAIreasoning,STRING_AGG(Riskcategory, ',') AS Riskcategory FROM (
    Select im.id as InterviewId,'deficitmorethan10' as Type,'High Severity' as Riskcategory      
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
    join IRF on IRF.IRFId=im.IRFId      
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType      
    where (MeetingStatus=4) and DateDiff(Day,im.interviewDate,Getdate())<30      
    and (ic.VideoDuration-pt.DurationSelectedMin)>(-pt.DurationSelectedMin) and (ic.VideoDuration-pt.DurationSelectedMin)<-10      
    union      
    Select im.id as InterviewId,'AIVerdictLessthan4' as Type,'High Severity' as Riskcategory      
    From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
    join IRF on IRF.IRFId=im.IRFId      
    join Client cl on cl.id=IRF.ClientId      
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and interviewDate between @StartDate and @EndDate      
    and ic.AITranscriptAnalysisScore<4      
    union      
    Select im.id as InterviewId,'CandidateRating' as Type,'High Severity' as Riskcategory      
    From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
    join IRF on IRF.IRFId=im.IRFId      
    join Client cl on cl.id=IRF.ClientId      
    join CandidateFeedbackDetails cfd on cfd.InterviewId=im.id      
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and interviewDate between @StartDate and @EndDate  and cfd.Rating<4
    union
    Select im.id as InterviewId,'Selectdeficit' as Type,'Under Review' as Riskcategory        
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
    join IRF on IRF.IRFId=im.IRFId      
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType      
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and interviewDate between @StartDate and @EndDate      
    and (pt.DurationSelectedMin*2/3>ic.VideoDuration) and (InterviewStatus=2 or InterviewStatus=4)    
    union    
    Select im.id as InterviewId,'Rejectdeficit' as Type,'Under Review' as Riskcategory              
    from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id      
    join IRF on IRF.IRFId=im.IRFId      
    join PlanTypeChargesMaster pt on pt.CountryType=IRF.JobCountry and pt.PlanTypeEnumValue=IRF.Heuristicsscore and IRF.CompanyType=pt.CompanyType      
    where (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8) and interviewDate between @StartDate and @EndDate      
    and (pt.DurationSelectedMin*4/9>ic.VideoDuration) and (InterviewStatus=1 or InterviewStatus=3 or InterviewStatus=5)
    union
    Select im.id as InterviewId,Case when imj.Joinedon is null and imc.PerformedOn is null then 'No Show'   
    when imj.Joinedon is null then 'Interviewer No Show' when imc.PerformedOn is null then 'Candidate No Show' end as Type,'Fake Report' as Riskcategory
    from InterviewMeetings im  join InterviewComments ic on ic.InterviewId=im.id                  
    outer apply(Select max(Joinedon) Joinedon from  InterviewMeetingsJoinTime where Interviewid=im.id) imj            
    outer apply(Select max(PerformedOn) PerformedOn From PageClickDetails where ButtonAction='JoinMeeting' and Interviewid=im.id and ProfileType='Candidate') imc            
    where interviewDate between @StartDate and @EndDate and im.confirmationStatus=1 and im.ZoomMeetingId in(Select distinct Meetingid from ZoomMeetingTransmission) and (imj.Joinedon is null or imc.PerformedOn is null) and ic.MeetingStatus!=5 and ic.MeetingStatus!=1 and ic.MeetingStatus!=9 and ic.MeetingStatus!=2 and ic.MeetingStatus!=3)x GROUP BY InterviewId) d ON d.InterviewId = im.id
    where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate between @StartDate and @EndDate and DATEDIFF(DAY, interviewDate,@currentdate)<7 and ISNULL(im.RedoInterview,0)=0 and ISNULL(IRF.IsAIInterview,0)=0
 end    
  end    
  else    
  begin    
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,ic.AIVerdict,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,uli.MobileNumber as InterviewerMobileNumber,ulco.EmailId as ChannelOwner  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 
left join UserList ulco on IRF.RisebirdHr=ulco.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ISNULL(im.RedoInterview,0)=0 and ul.EmailId=@emailID   
   and interviewDate between @StartDate and @EndDate and ISNULL(IRF.IsAIInterview,0)=0  
   union   
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,ic.AIVerdict,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,uli.MobileNumber as InterviewerMobileNumber,ulco.EmailId as ChannelOwner  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 left join UserList ulco on IRF.RisebirdHr=ulco.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ISNULL(im.RedoInterview,0)=0 and ul.EmailId=@emailID  
  and interviewDate between @StartDate and @EndDate and ISNULL(IRF.IsAIInterview,0)=0  
  end    
 end    
 else If(@actionstatus ='QCReport')    
 begin    
  if (@actiontype = 0)    
  begin    
   if (@Profile = 'SeniorHR')    
   begin    
    Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags,ulco.EmailId as ChannelOwner from InterviewMeetings im join IRF 
on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join 
 CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id left join UserList ulco on IRF.RisebirdHr=ulco.id  where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ulco.Emailid= @Emailid  and interviewDate between @StartDate and @EndDate  
 end    
   else    
   begin    
    Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,tq.PaymentComplianceCount as Redflags,IRF.id as irfIntId,ulco.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1    
 left join UserList ulco on IRF.RisebirdHr=ulco.id  
 where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate between @StartDate and @EndDate  
   end    
  end    
  else    
  begin    
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,tq.PaymentComplianceCount as Redflags,IRF.id as irfIntId,ulco.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.
UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 left join UserList ulco on IRF.RisebirdHr=ulco.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
   union   
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,tq.PaymentComplianceCount as Redflags,IRF.id as irfIntId,ulco.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail
 join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 left join UserList ulco on IRF.RisebirdHr=ulco.id where IsNull(IsProctor,0)=0 and 
(ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
  end    
 end    
  
  
 If(@actionstatus ='ReScheduleCount')    
 begin    
  if (@actiontype = 0)    
  begin    
   if (@Profile = 'SeniorHR' or @Profile = 'HR')    
   begin    
 select distinct im.*,IRF.Id as irfIntId,ul.EmailId as ChannelOwner  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
 join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid   
 join UserList ul on ul.id=irf.RisebirdHr   
 where confirmationStatus=2 and ul.Emailid= @Emailid and interviewDate between @StartDate and @EndDate  
   end    
   else    
   begin    
    select distinct im.*,IRF.Id as irfIntId,ul.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join UserList ul on ul.id=irf.RisebirdHr  where confirmationStatus=2 and interviewDate between @StartDate and @EndDate  
   end    
  end    
  else    
  begin    
   select distinct im.*,irf.*,cl.*,ulc.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id     
   join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId   
   join UserList ulc on ul.id=irf.RisebirdHr   
   where confirmationStatus=2 and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate     
   union     
   select distinct im.*,irf.*,cl.*,ulc.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr     
   join UserList ulc on ul.id=irf.RisebirdHr   
   where confirmationStatus=2 and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
  end    
 end  
  
  else If(@actionstatus ='PotentialRiskByClient')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
    select distinct ccfr.Comment,ccfr.Interviewid,im.*, cl.Name,ul.EmailId as ChannelOwner,IRF.id as irfIntId ,ccfr.Rating,ccfr.CreatedAt as RateAddedOn, ccfr.CreatedBy as RateAddedBy,ic.InterviewStatus,tq.Redflags,ic.MeetingStatus  
     ,ps.IsApproved,itp.RedMarker,ps.PaymentComplianceCount  
     from  ClientCommentsForReport ccfr   
     join InterviewMeetings im on ccfr.InterviewId=im.id  
     join InterviewComments ic on ic.InterviewId=im.id   
       left join Trans_QCFormReport tq on tq.Interviewid=im.id  
    join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
    join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid   
    join UserList ul on ul.id=  irf.RisebirdHr  
    left join PaymentSLADetails ps on im.id=ps.Interviewid  
    left join InterviewerProfile itp on itp.UserId = ul.id  
    where ccfr.Rating<=3 and ul.Emailid= @Emailid and interviewDate between @StartDate and @EndDate  
   end  
   else  
   begin  
    select distinct ccfr.Comment,ccfr.Interviewid,im.*, cl.Name,ul.EmailId as ChannelOwner, IRF.id as irfIntId,ccfr.Rating,ccfr.CreatedAt as RateAddedOn, ccfr.CreatedBy as RateAddedBy  
    ,ic.InterviewStatus,tq.Redflags,ic.MeetingStatus,ps.PaymentComplianceCount,ps.IsApproved,itp.RedMarker  
    from  ClientCommentsForReport ccfr   
    join InterviewMeetings im on ccfr.InterviewId=im.id  
    join InterviewComments ic on ic.InterviewId=im.id   
       left join Trans_QCFormReport tq on tq.Interviewid=im.id   
    left join PaymentSLADetails ps on ps.Interviewid=im.id  
    join IRF on im.IRFId=irf.IRFId  join Client cl on irf.ClientId=cl.id   
    join UserList ul on ul.id=  irf.RisebirdHr  
    left join InterviewerProfile itp on itp.UserId = ul.id  
    where  ccfr.Rating<=3 and interviewDate between @StartDate and @EndDate  
   end  
  end  
  else  
  begin  
   select distinct ccfr.Comment,ccfr.Interviewid,im.*,cl.Name,ulc.EmailId as ChannelOwner,IRF.id as irfIntId,ccfr.Rating,ccfr.CreatedAt as RateAddedOn,ccfr.CreatedBy as RateAddedBy,ic.InterviewStatus,tq.Redflags,ic.MeetingStatus, ps.PaymentComplianceCount,ps.IsApproved  
   ,itp.RedMarker  
   from  ClientCommentsForReport ccfr   
   join InterviewMeetings im on ccfr.InterviewId=im.id  
   join InterviewComments ic on ic.InterviewId=im.id   
   left join PaymentSLADetails ps on ps.Interviewid=im.id  
   left join Trans_QCFormReport tq on tq.Interviewid=im.id  
   join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId  
   join Userlist ul on ul.id=HR.UserId   
   join UserList ulc on ulc.id=  irf.RisebirdHr  
   left join InterviewerProfile itp on itp.UserId = ul.id  
   where ccfr.Rating<=3 and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
   union   
   select distinct ccfr.Comment,ccfr.Interviewid,im.*,cl.Name,ul.EmailId as ChannelOwner,IRF.id as irfIntId,ccfr.Rating,ccfr.CreatedAt as RateAddedOn,ccfr.CreatedBy as RateAddedBy,ic.InterviewStatus,tq.Redflags,ic.MeetingStatus,ps.PaymentComplianceCount,ps.IsApproved  
   ,itp.RedMarker  
   from  ClientCommentsForReport ccfr   
    join InterviewMeetings im on ccfr.InterviewId=im.id  
    join InterviewComments ic on ic.InterviewId=im.id   
    left join PaymentSLADetails ps on ps.Interviewid=im.id  
       left join Trans_QCFormReport tq on tq.Interviewid=im.id  
    join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
    join Userlist ul on ul.id=irf.RisebirdHr   
    left join InterviewerProfile itp on itp.UserId = ul.id  
   where ccfr.Rating<=3 and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
  end  
 end  
  
  else If(@actionstatus ='NeedClarity')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
    Select distinct im.*,im.id as InterviewId,cl.Name,ul.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=
sh.Userid left join UserList ul on ul.id=  irf.RisebirdHr where CandidateResponse=4 and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate  
   end  
   else  
   begin  
    Select distinct im.*,im.id as InterviewId,cl.Name,ul.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join UserList ul on ul.id=  irf.RisebirdHr where CandidateResponse=4 and interviewDate between @StartDate and @EndDate  
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.*,ic.*,uli.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId left join UserList uli on uli.id=  irf.RisebirdHr where CandidateResponse=4 and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
   union   
   select distinct im.*,irf.*,cl.*,ic.*,ul.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where CandidateResponse=4 and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
  end  
 end  
  else If(@actionstatus ='CancelledInterviewCount')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
    select distinct im.*, cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId from InterviewMeetings im   
    join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
    join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid   
    join UserList ul on ul.id=  irf.RisebirdHr   
    where  confirmationStatus=2 and  interviewDate between @StartDate and @EndDate and ul.EmailId=@emailID and im.RedoInterview=0  
   end  
   else  
   begin  
    select distinct im.*, cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId from InterviewMeetings im   
    join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
    join UserList ul on ul.id=  irf.RisebirdHr  
    where confirmationStatus=2 and  interviewDate between @StartDate and @EndDate and im.RedoInterview=0  
   end  
  end  
  else  
  begin  
   select distinct im.*,cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId   
   join UserList ulc on ulc.id=  irf.RisebirdHr  
   where confirmationStatus=2 and  interviewDate between @StartDate and @EndDate and ul.EmailId=@emailID and im.RedoInterview=0  
   union   
   select distinct im.*,cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr   
   join UserList ulc on ulc.id=  irf.RisebirdHr  
   where confirmationStatus=2 and  interviewDate between @StartDate and @EndDate and ul.EmailId=@emailID and im.RedoInterview=0  
  end  
 end  
  
END
GO