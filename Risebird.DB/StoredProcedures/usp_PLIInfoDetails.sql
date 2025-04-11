IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_PLIInfoDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_PLIInfoDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_PLIInfoDetails  
@timeNow datetime,  
@type varchar(50),  
@EmailId varchar(150)  
AS  
BEGIN  
Declare @Role varchar(150)                
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid  
  
declare @yesterdayDate date = cast(dateadd(day, -1, @timeNow) as date)    
  
DECLARE @tbl_RelevantIRFs TABLE (                        
 id int,  
 IRFId varchar(15)  
)    
    
INSERT INTO @tbl_RelevantIRFs        
select IRF.id, im.IRFId  from InterviewMeetings im    
join IRF on IRF.IRFId=im.IRFId  
where (cast(im.interviewDate as date)>= @yesterdayDate) and confirmationStatus = 1   
  
DECLARE @tbl_RelevantInterviews TABLE (                        
 IRFId varchar(15),  
 Interviewer varchar(150),  
 TotalInterview int  
)    
  
Insert into @tbl_RelevantInterviews  
Select distinct ir.IRFId,im.interviewer,Count(distinct im.id) TotalInterview from @tbl_RelevantIRFs ir   
join TransInterviewer ti on ti.IRFId=ir.id  
join UserList ul on ul.id=ti.UserId  
join InterviewMeetings im on im.IRFId=ir.IRFId and im.interviewer=ul.EmailId   
group by ir.IRFId,im.interviewer  
  
  
DECLARE @tbl_InterviewerCompleted TABLE (                        
    IRFId varchar(15),  
    interviewer varchar(200),    
    TotalCompletedMeetings int    
);     
    
INSERT INTO @tbl_InterviewerCompleted        
select ir.IRFId,im.interviewer, count(distinct im.id) from @tbl_RelevantIRFs ir  
join InterviewMeetings im on im.IRFId=ir.IRFId  
left join InterviewComments ic on ic.InterviewId = im.id    
where (ic.MeetingStatus in (4, 6, 7, 8))    
group by ir.IRFId,im.interviewer    
  
if(@type Like '%Shortselectcomment%')    
Begin  
if(@Role='SeniorHR')  
Begin  
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotallessComment  
from @tbl_RelevantIRFs ir join Interviewmeetings im on im.IRFId=ir.IRFId  
join InterviewComments ic on im.id=ic.InterviewId   
where confirmationStatus=1   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)   
and (InterviewStatus=2 or InterviewStatus=4)  
and Len(ic.InterviewerComment)<500   
group by im.IRFId,im.interviewer)  
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,imt.TotalInterview,icm.TotalCompletedMeetings,CTE.TotallessComment   
from CTE  
left join @tbl_RelevantInterviews imt on imt.IRFId=CTE.IRFId and imt.Interviewer=CTE.interviewer  
join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on IRF.RisebirdHr=ul.id  
join Client cl on cl.id=IRF.Clientid  
left join UserList ulc on ulc.id=IRF.BillingOwner  
left join UserList uli on uli.Emailid=CTE.interviewer  
left join InterviewerProfile ip on ip.UserId=uli.id  
left join @tbl_InterviewerCompleted icm on icm.IRFId=CTE.IRFId and icm.interviewer=CTE.interviewer  
where ul.EmailId=@EmailId  
End  
Else  
Begin  
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotallessComment  
from @tbl_RelevantIRFs ir join Interviewmeetings im on im.IRFId=ir.IRFId  
join InterviewComments ic on im.id=ic.InterviewId   
where confirmationStatus=1   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)   
and (InterviewStatus=2 or InterviewStatus=4)  
and Len(ic.InterviewerComment)<500   
group by im.IRFId,im.interviewer)  
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,imt.TotalInterview,icm.TotalCompletedMeetings,CTE.TotallessComment   
from CTE  
left join @tbl_RelevantInterviews imt on imt.IRFId=CTE.IRFId and imt.Interviewer=CTE.interviewer  
join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on IRF.RisebirdHr=ul.id  
join Client cl on cl.id=IRF.Clientid  
left join UserList ulc on ulc.id=IRF.BillingOwner  
left join UserList uli on uli.Emailid=CTE.interviewer  
left join InterviewerProfile ip on ip.UserId=uli.id  
left join @tbl_InterviewerCompleted icm on icm.IRFId=CTE.IRFId and icm.interviewer=CTE.interviewer  
End  
END  
else if(@type Like '%Shortrejectcomment%')  
begin  
if(@Role='SeniorHR')  
Begin  
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotallessComment  
from @tbl_RelevantIRFs ir join Interviewmeetings im on im.IRFId=ir.IRFId  
join InterviewComments ic on im.id=ic.InterviewId   
where confirmationStatus=1   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)  
and (InterviewStatus=3 or InterviewStatus=5)  
and Len(ic.InterviewerComment)<250  
group by im.IRFId,im.interviewer)  
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,imt.TotalInterview,icm.TotalCompletedMeetings,CTE.TotallessComment   
from CTE  
left join @tbl_RelevantInterviews imt on imt.IRFId=CTE.IRFId and imt.Interviewer=CTE.interviewer  
join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on IRF.RisebirdHr=ul.id  
join Client cl on cl.id=IRF.Clientid  
left join UserList ulc on ulc.id=IRF.BillingOwner  
left join UserList uli on uli.Emailid=CTE.interviewer  
left join InterviewerProfile ip on ip.UserId=uli.id  
left join @tbl_InterviewerCompleted icm on icm.IRFId=CTE.IRFId and icm.interviewer=CTE.interviewer  
where ul.EmailId=@EmailId  
End  
Else  
Begin  
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotallessComment  
from @tbl_RelevantIRFs ir join Interviewmeetings im on im.IRFId=ir.IRFId  
join InterviewComments ic on im.id=ic.InterviewId   
where confirmationStatus=1   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)  
and (InterviewStatus=3 or InterviewStatus=5)  
and Len(ic.InterviewerComment)<250  
group by im.IRFId,im.interviewer)  
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,imt.TotalInterview,icm.TotalCompletedMeetings,CTE.TotallessComment   
from CTE  
left join @tbl_RelevantInterviews imt on imt.IRFId=CTE.IRFId and imt.Interviewer=CTE.interviewer  
join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on IRF.RisebirdHr=ul.id  
join Client cl on cl.id=IRF.Clientid  
left join UserList ulc on ulc.id=IRF.BillingOwner  
left join UserList uli on uli.Emailid=CTE.interviewer  
left join InterviewerProfile ip on ip.UserId=uli.id  
left join @tbl_InterviewerCompleted icm on icm.IRFId=CTE.IRFId and icm.interviewer=CTE.interviewer  
End  
end  
else if(@type Like '%Lessquestion%')  
begin  
if(@Role='SeniorHR')  
Begin  
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotallessQuestions  
from @tbl_RelevantIRFs ir join InterviewMeetings im on im.IRFId=ir.IRFId join QuestionsDiscussed qd on im.id=qd.InterviewId    
join InterviewComments ic on im.id=ic.InterviewId     
where confirmationStatus=1   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)  
group by im.IRFId,im.interviewer  having Count(qd.id)<5)  
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,imt.TotalInterview,icm.TotalCompletedMeetings,CTE.TotallessQuestions   
from CTE  
left join @tbl_RelevantInterviews imt on imt.IRFId=CTE.IRFId and imt.Interviewer=CTE.interviewer  
join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on IRF.RisebirdHr=ul.id  
join Client cl on cl.id=IRF.Clientid  
left join UserList ulc on ulc.id=IRF.BillingOwner  
left join UserList uli on uli.Emailid=CTE.interviewer  
left join InterviewerProfile ip on ip.UserId=uli.id  
left join @tbl_InterviewerCompleted icm on icm.IRFId=CTE.IRFId and icm.interviewer=CTE.interviewer  
where ul.EmailId=@EmailId  
End  
Else  
Begin  
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotallessQuestions  
from @tbl_RelevantIRFs ir join InterviewMeetings im on im.IRFId=ir.IRFId join QuestionsDiscussed qd on im.id=qd.InterviewId    
join InterviewComments ic on im.id=ic.InterviewId     
where confirmationStatus=1   
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)  
group by im.IRFId,im.interviewer  having Count(qd.id)<5)  
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,imt.TotalInterview,icm.TotalCompletedMeetings,CTE.TotallessQuestions   
from CTE  
left join @tbl_RelevantInterviews imt on imt.IRFId=CTE.IRFId and imt.Interviewer=CTE.interviewer  
join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on IRF.RisebirdHr=ul.id  
join Client cl on cl.id=IRF.Clientid  
left join UserList ulc on ulc.id=IRF.BillingOwner  
left join UserList uli on uli.Emailid=CTE.interviewer  
left join InterviewerProfile ip on ip.UserId=uli.id  
left join @tbl_InterviewerCompleted icm on icm.IRFId=CTE.IRFId and icm.interviewer=CTE.interviewer  
End  
end  
else if(@type Like '%Latejoin%')  
begin  
if(@Role='SeniorHR')  
Begin  
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotalLatejoin  
from @tbl_RelevantIRFs ir join InterviewMeetings im on im.IRFId=ir.IRFId  
join InterviewMeetingsJoinTime imj on im.id=imj.interviewid join InterviewComments ic on ic.Interviewid=im.id       
where DATEDIFF(MINUTE,interviewDate,Joinedon)>5 and (ic.MeetingStatus=1 or ic.MeetingStatus=5 or ic.MeetingStatus=9)  
group by im.IRFId,im.interviewer)  
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,imt.TotalInterview,icm.TotalCompletedMeetings,CTE.TotalLatejoin   
from CTE  
left join @tbl_RelevantInterviews imt on imt.IRFId=CTE.IRFId and imt.Interviewer=CTE.interviewer  
join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on IRF.RisebirdHr=ul.id  
join Client cl on cl.id=IRF.Clientid  
left join UserList ulc on ulc.id=IRF.BillingOwner  
left join UserList uli on uli.Emailid=CTE.interviewer  
left join InterviewerProfile ip on ip.UserId=uli.id  
left join @tbl_InterviewerCompleted icm on icm.IRFId=CTE.IRFId and icm.interviewer=CTE.interviewer  
where ul.EmailId=@EmailId  
End  
Else  
Begin  
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotalLatejoin  
from @tbl_RelevantIRFs ir join InterviewMeetings im on im.IRFId=ir.IRFId  
join InterviewMeetingsJoinTime imj on im.id=imj.interviewid join InterviewComments ic on ic.Interviewid=im.id       
where DATEDIFF(MINUTE,interviewDate,Joinedon)>5 and (ic.MeetingStatus=1 or ic.MeetingStatus=5 or ic.MeetingStatus=9)  
group by im.IRFId,im.interviewer)  
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,imt.TotalInterview,icm.TotalCompletedMeetings,CTE.TotalLatejoin   
from CTE  
left join @tbl_RelevantInterviews imt on imt.IRFId=CTE.IRFId and imt.Interviewer=CTE.interviewer  
join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on IRF.RisebirdHr=ul.id  
join Client cl on cl.id=IRF.Clientid  
left join UserList ulc on ulc.id=IRF.BillingOwner  
left join UserList uli on uli.Emailid=CTE.interviewer  
left join InterviewerProfile ip on ip.UserId=uli.id  
left join @tbl_InterviewerCompleted icm on icm.IRFId=CTE.IRFId and icm.interviewer=CTE.interviewer  
End  
end  

else if(@type Like '%Lessduration%')  
begin  
if(@Role='SeniorHR')  
Begin  
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotalLessduration
from @tbl_RelevantIRFs ir join InterviewMeetings im on im.IRFId=ir.IRFId  
join IRF on IRF.IRFId=ir.IRFId
join PlanTypeChargesMaster pt on pt.CompanyType=IRF.CompanyType and pt.PlanTypeEnumValue=IRF.Heuristicsscore and pt.CountryType=IRF.JobCountry
join InterviewComments ic on ic.Interviewid=im.id       
where (ic.VideoDuration-pt.DurationSelectedMin<0 and ic.VideoDuration-pt.DurationSelectedMin>-pt.DurationSelectedMin) and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)  
group by im.IRFId,im.interviewer)  
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,imt.TotalInterview,icm.TotalCompletedMeetings,CTE.TotalLessduration   
from CTE  
left join @tbl_RelevantInterviews imt on imt.IRFId=CTE.IRFId and imt.Interviewer=CTE.interviewer  
join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on IRF.RisebirdHr=ul.id  
join Client cl on cl.id=IRF.Clientid  
left join UserList ulc on ulc.id=IRF.BillingOwner  
left join UserList uli on uli.Emailid=CTE.interviewer  
left join InterviewerProfile ip on ip.UserId=uli.id  
left join @tbl_InterviewerCompleted icm on icm.IRFId=CTE.IRFId and icm.interviewer=CTE.interviewer  
where ul.EmailId=@EmailId and uli.IsActive=1
End  
Else  
Begin  
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct im.id) as TotalLessduration  
from @tbl_RelevantIRFs ir join InterviewMeetings im on im.IRFId=ir.IRFId  
join IRF on IRF.IRFId=ir.IRFId
join PlanTypeChargesMaster pt on pt.CompanyType=IRF.CompanyType and pt.PlanTypeEnumValue=IRF.Heuristicsscore and pt.CountryType=IRF.JobCountry
join InterviewComments ic on ic.Interviewid=im.id       
where (ic.VideoDuration-pt.DurationSelectedMin<0 and ic.VideoDuration-pt.DurationSelectedMin>-pt.DurationSelectedMin) and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)  
group by im.IRFId,im.interviewer)  
select IRF.id,CTE.IRFId,CTE.interviewer,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,imt.TotalInterview,icm.TotalCompletedMeetings,CTE.TotalLessduration   
from CTE  
left join @tbl_RelevantInterviews imt on imt.IRFId=CTE.IRFId and imt.Interviewer=CTE.interviewer  
join IRF on IRF.IRFId=CTE.IRFId  
join UserList ul on IRF.RisebirdHr=ul.id  
join Client cl on cl.id=IRF.Clientid  
left join UserList ulc on ulc.id=IRF.BillingOwner  
left join UserList uli on uli.Emailid=CTE.interviewer  
left join InterviewerProfile ip on ip.UserId=uli.id  
left join @tbl_InterviewerCompleted icm on icm.IRFId=CTE.IRFId and icm.interviewer=CTE.interviewer  
where uli.IsActive=1
End  
end  
END  
GO