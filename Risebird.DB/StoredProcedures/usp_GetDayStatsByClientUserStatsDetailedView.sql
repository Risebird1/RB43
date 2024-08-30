IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetDayStatsByClientUserStatsDetailedView'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDayStatsByClientUserStatsDetailedView
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetDayStatsByClientUserStatsDetailedView
@irfid int,
@startdate datetime,  
@enddate datetime,  
@timeNow datetime,  
@type varchar(50)
AS  
BEGIN  
Declare @AllScheduleinterview as Table(  
interviewdate datetime,  
id int,  
candidateEmail varchar(150),  
confirmationStatus int,
IRFId varchar(15)
)  
  
Insert into @AllScheduleinterview  
Select interviewDate,im.id,im.candidateEmail,im.confirmationStatus,IRF.IRFId From InterviewMeetings im  
join IRF on IRF.IRFId=im.IRFId  
where interviewDate>Convert(date,@startDate) and interviewDate<Convert(date,@endDate) and IRF.id=@irfid

if(@type='Completed')      
begin      
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,     
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,    
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo,IRF.IRFId    
From @AllScheduleinterview imc
join InterviewMeetings im on im.id=imc.id
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on im.id=ic.InterviewId and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone    
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo    
end
if(@type='Selected')      
begin      
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,     
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,    
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo,IRF.IRFId    
From @AllScheduleinterview imc
join InterviewMeetings im on im.id=imc.id
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on im.id=ic.InterviewId and (ic.InterviewStatus=2 or ic.InterviewStatus=4)  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone    
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo    
end
if(@type='Rejected')      
begin      
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,     
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,    
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo,IRF.IRFId    
From @AllScheduleinterview imc
join InterviewMeetings im on im.id=imc.id
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on im.id=ic.InterviewId and (ic.InterviewStatus=3 or ic.InterviewStatus=5)  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone    
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo    
end
else if(@type='Futureschedule')      
begin      
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,     
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,    
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo,IRF.IRFId    
From @AllScheduleinterview imc
join InterviewMeetings im on im.id=imc.id
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on im.id=ic.InterviewId and im.confirmationStatus=1 and im.interviewdate>@timeNow  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone    
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo    
end
else if(@type='Pending')      
begin      
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,     
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,    
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo,IRF.IRFId    
From @AllScheduleinterview imc
join InterviewMeetings im on im.id=imc.id
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on im.id=ic.InterviewId and im.confirmationStatus=1 and im.interviewdate<@timeNow and (ic.MeetingStatus=2 or ic.MeetingStatus is null)  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone    
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo    
end
else if(@type='Reschedule')      
begin      
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,     
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,    
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo,IRF.IRFId    
From @AllScheduleinterview imc
join InterviewMeetings im on im.id=imc.id
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on im.id=ic.InterviewId and (im.confirmationStatus=2 or ic.MeetingStatus=1 or ic.MeetingStatus=3 or ic.MeetingStatus=5 or ic.MeetingStatus=9)  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone    
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo    
end
END 
GO