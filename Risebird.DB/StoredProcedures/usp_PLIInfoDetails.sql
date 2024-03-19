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
@type varchar(50)
AS
BEGIN
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
END
else if(@type Like '%Shortrejectcomment%')
begin
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
end
else if(@type Like '%Lessquestion%')
begin
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
end
else if(@type Like '%Latejoin%')
begin
;with CTE as (Select im.IRFId,im.interviewer,Count(distinct imj.id) as TotalLatejoin
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
end
END
GO