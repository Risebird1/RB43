IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_PQIHighStatsRates'))
BEGIN
    DROP PROCEDURE dbo.usp_PQIHighStatsRates
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_PQIHighStatsRates
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

if(@type Like '%Selection%')  
Begin

DECLARE @tbl_InterviewerSelected TABLE (                      
        IRFId varchar(15),
		interviewer varchar(200),  
        TotalSelected int  
);  
  
INSERT INTO @tbl_InterviewerSelected    
select ir.IRFId,im.interviewer, count(distinct im.id) from @tbl_RelevantIRFs ir
join InterviewMeetings im on im.IRFId=ir.IRFId
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.InterviewStatus = 2) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 6)  
group by ir.IRFId,im.interviewer  
  
;With CTE as(
Select IRF.id,im.IRFId,im.interviewer,im.TotalInterview,icm.TotalCompletedMeetings,ism.TotalSelected,(ISNULL(ism.TotalSelected,0)*100/icm.TotalCompletedMeetings) selectionrate,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId 
from @tbl_RelevantInterviews im
join IRF on IRF.IRFId=im.IRFId
join UserList ul on IRF.RisebirdHr=ul.id
join Client cl on cl.id=IRF.Clientid
left join UserList uli on uli.Emailid=im.interviewer
left join @tbl_InterviewerCompleted icm on icm.IRFId=im.IRFId and icm.interviewer=im.interviewer
left join @tbl_InterviewerSelected ism on ism.IRFId=im.IRFId and ism.interviewer=im.interviewer)
Select * From CTE where selectionrate>=60
END

Else 
begin
DECLARE @tbl_InterviewerRejected TABLE (                      
        IRFId varchar(15),
		interviewer varchar(200),  
        TotalRejected int  
);  
  
INSERT INTO @tbl_InterviewerRejected    
select ir.IRFId,im.interviewer, count(distinct im.id) from @tbl_RelevantIRFs ir
join InterviewMeetings im on im.IRFId=ir.IRFId
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.InterviewStatus = 3) and (ic.MeetingStatus = 4  or ic.MeetingStatus = 7)  
group by ir.IRFId,im.interviewer  
  
;With CTE as(
Select IRF.id,im.IRFId,im.interviewer,im.TotalInterview,icm.TotalCompletedMeetings,ism.TotalRejected,(ISNULL(ism.TotalRejected,0)*100/icm.TotalCompletedMeetings) rejectionrate,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId 
from @tbl_RelevantInterviews im
join IRF on IRF.IRFId=im.IRFId
join UserList ul on IRF.RisebirdHr=ul.id
join Client cl on cl.id=IRF.Clientid
left join UserList uli on uli.Emailid=im.interviewer
left join @tbl_InterviewerCompleted icm on icm.IRFId=im.IRFId and icm.interviewer=im.interviewer
left join @tbl_InterviewerRejected ism on ism.IRFId=im.IRFId and ism.interviewer=im.interviewer)
Select * From CTE where rejectionrate>=60
end
END

GO