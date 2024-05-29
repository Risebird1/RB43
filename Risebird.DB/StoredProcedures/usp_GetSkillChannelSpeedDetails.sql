IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSkillChannelSpeedDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelSpeedDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_GetSkillChannelSpeedDetails        
@irfId int,        
@type varchar(20)        
AS        
BEGIN        
--Report Pending      
if(@type='ReportPending')        
begin        
select im.interviewDate,im.interviewer,im.candidateEmail,im.candidateName,im.MobileNumber,IRF.id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id       
left join InterviewComments ic on ic.InterviewId= im.id       
where IRF.id=@irfId and  confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewdate<DATEADD(Minute,150,Getdate())       
end      
--Proxy Interviews    
else if(@type='ProxyInterview')        
begin    
Select im.interviewDate,im.interviewer,im.candidateEmail,im.candidateName,im.MobileNumber,IRF.id as irfIntId,im.id as imid From InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId     
join IRF on IRF.IRFId=im.IRFId    
where ic.InterviewStatus=1 and IRF.id=@irfId and confirmationStatus=1    
end    
--Reschedule Interviews  
else if(@type='Reschedule')      
begin  
Declare @temp as table   
(  
IRFId varchar(15),  
candidateEmail varchar(150)  
)  
Declare @tempfinal as table   
(  
IRFId varchar(15),  
candidateEmail varchar(150),  
interviewDate datetime,  
MeetingStatus int,  
confirmationStatus int  
)  
Declare @temp1 as table   
(  
IRFId varchar(15),  
candidateEmail varchar(150),  
interviewdate datetime  
)  
Declare @temp2 as table   
(  
IRFId varchar(15),  
candidateEmail varchar(150),  
interviewdate datetime  
)  
  
;with CTE as(Select im.IRFID,im.candidateEmail,MAx(im.interviewDate) interviewedate   
from  InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id join  IRF on IRF.IRFId=im.IRFId  
where IRF.id =@irfId and im.confirmationStatus=2  
group by im.IRFID,im.candidateEmail)  
Insert into @temp1  
Select im.IRFId,im.candidateEmail,interviewedate From CTE join InterviewMeetings im on im.interviewDate=CTE.interviewedate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId  
where ConfirmationStatus=2  
  
Insert into @temp2  
Select im.IRFId,im.candidateEmail,max(interviewDate) interviewdate   
From InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id join  IRF on IRF.IRFId=im.IRFId  
where IRF.id =@irfId and (ic.MeetingStatus=1 or ic.MeetingStatus=5 or ic.MeetingStatus=9 or ic.MeetingStatus=3)  
group by im.IRFID,im.candidateEmail  
  
Insert into @temp  
Select IRFId,candidateEmail From @temp1  
union  
Select IRFId,candidateEmail From @temp2  
  
;with CTE as(Select tp.IRFId,tp.candidateEmail,Max(im.interviewDate) interviewDate from @temp tp join InterviewMeetings im on im.IRFId=tp.IRFId and im.candidateEmail=tp.candidateEmail  
join InterviewComments ic on ic.InterviewId=im.id  
group by tp.IRFId,tp.candidateEmail)  
Insert into @tempfinal  
Select im.IRFId,im.candidateEmail,CTE.interviewDate,ic.MeetingStatus,im.confirmationStatus from CTE join InterviewMeetings im on im.interviewDate=CTE.interviewdate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId  
join InterviewComments ic on ic.InterviewId=im.id where (ic.MeetingStatus!=4 and ic.MeetingStatus!=6 and ic.MeetingStatus!=7 and ic.MeetingStatus!=8 )  
order by CTE.candidateEmail  
  
;with CTE as (Select distinct tp.IRFId,tp.candidateEmail,tp.interviewdate from @tempfinal tp join InterviewMeetings im on im.IRFId=tp.IRFId and im.candidateEmail=tp.candidateEmail  
join InterviewComments ic on ic.InterviewId=im.id  
where ((tp.confirmationStatus=1 and tp.MeetingStatus!=2) or (tp.confirmationStatus=2)) and   
im.candidateEmail not in(Select distinct tp.candidateEmail from @tempfinal tp join InterviewMeetings im on im.IRFId=tp.IRFId and im.candidateEmail=tp.candidateEmail  
join InterviewComments ic on ic.InterviewId=im.id  
where ic.MeetingStatus=4))  
Select CTE.interviewdate,CTE.candidateEmail,ca.ResumeLink,ul.MobileNumber,IRF.CalendlyLink,cm.id as CandidateId,IRF.id as irfIntId From CTE join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on ul.EmailId=CTE.candidateEmail  
join CandidatesMaster cm on cm.UserId=ul.id  
join Candidate ca on ca.IRFId=IRF.id and ca.CandidateMasterId=cm.id  
where DateDiff(Day,Interviewdate,DateAdd(Minute,330,Getdate()))>=2  
order by candidateEmail  
end  
--Qc Pending for selected candidate   
else if(@type='QcPending')        
begin    
Select im.interviewDate,im.interviewer,im.candidateEmail,im.candidateName,im.MobileNumber,IRF.id as irfIntId,im.id as imid FRom InterviewMeetings im join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on ic.interviewid=im.id
left join PaymentSLADetails psa on psa.Interviewid=im.id
where IRF.id=@irfId and confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and ISNULL(psa.isQcDone,0)=0
and DATEDIFF(HOUR,interviewDate,DateAdd(Minute,330,Getdate()))>12
end    
END 
GO