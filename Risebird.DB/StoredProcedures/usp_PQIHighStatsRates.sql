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
@uniqueid varchar(50),
@type varchar(50)
AS
BEGIN
Declare @Emailid varchar(150)
if(@uniqueid!=null or @uniqueid!='')
begin
Select @Emailid=Emailid from ClientPublicURLs where uniqueid=@uniqueid
end
declare @yesterdayDate date = cast(dateadd(day, -1, @timeNow) as date)  

DECLARE @tbl_RelevantIRFs TABLE (                      
 id int,
 IRFId varchar(15)
)  
  
--INSERT INTO @tbl_RelevantIRFs      
--select distinct IRF.id, im.IRFId  from InterviewMeetings im  
--join IRF on IRF.IRFId=im.IRFId
--where (cast(im.interviewDate as date)>= @yesterdayDate) and confirmationStatus = 1 

INSERT INTO @tbl_RelevantIRFs      
select distinct IRF.id, im.IRFId  from InterviewMeetings im  
join IRF on IRF.IRFId=im.IRFId
left join TransClientUser tc on tc.IRFId=IRF.id
left join UserList ul on tc.ClientUserId=ul.id 
where (cast(im.interviewDate as date)>= @yesterdayDate) and confirmationStatus = 1 and (@Emailid is null or ul.EmailId=@Emailid)

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
Select IRF.id,im.IRFId,im.interviewer,im.TotalInterview,icm.TotalCompletedMeetings,ism.TotalSelected,(ISNULL(ism.TotalSelected,0)*100/icm.TotalCompletedMeetings) selectionrate,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,ip.NickName 
from @tbl_RelevantInterviews im
join IRF on IRF.IRFId=im.IRFId
join UserList ul on IRF.RisebirdHr=ul.id
join Client cl on cl.id=IRF.Clientid
left join UserList ulc on ulc.id=IRF.BillingOwner
left join UserList uli on uli.Emailid=im.interviewer
left join InterviewerProfile ip on ip.UserId=uli.id
left join @tbl_InterviewerCompleted icm on icm.IRFId=im.IRFId and icm.interviewer=im.interviewer
left join @tbl_InterviewerSelected ism on ism.IRFId=im.IRFId and ism.interviewer=im.interviewer)
Select * From CTE where selectionrate>=40
END
else if(@type Like '%NegativeCandidate%')
begin
DECLARE @tbl_InterviewerNegativeRating TABLE (                      
        IRFId varchar(15),
		interviewer varchar(200),  
        NegativeRating int  
);  
  
INSERT INTO @tbl_InterviewerNegativeRating    
select ir.IRFId,im.interviewer, count(distinct im.id) from @tbl_RelevantIRFs ir
join InterviewMeetings im on im.IRFId=ir.IRFId
join CandidateFeedbackDetails cd on cd.InterviewId=im.id
where cd.Rating<4
group by ir.IRFId,im.interviewer  

;With CTE as(
Select IRF.id,im.IRFId,im.interviewer,im.TotalInterview,icm.TotalCompletedMeetings,ism.NegativeRating,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,ip.NickName 
from @tbl_RelevantInterviews im
join IRF on IRF.IRFId=im.IRFId
join UserList ul on IRF.RisebirdHr=ul.id
join Client cl on cl.id=IRF.Clientid
left join UserList ulc on ulc.id=IRF.BillingOwner
left join UserList uli on uli.Emailid=im.interviewer
left join InterviewerProfile ip on ip.UserId=uli.id
left join @tbl_InterviewerCompleted icm on icm.IRFId=im.IRFId and icm.interviewer=im.interviewer
left join @tbl_InterviewerNegativeRating ism on ism.IRFId=im.IRFId and ism.interviewer=im.interviewer)
Select * From CTE where NegativeRating>0
end

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
Select IRF.id,im.IRFId,im.interviewer,im.TotalInterview,icm.TotalCompletedMeetings,ism.TotalRejected,(ISNULL(ism.TotalRejected,0)*100/icm.TotalCompletedMeetings) rejectionrate,ul.EmailId as ChannelOwner,cl.Name as Client,uli.id as UserId,ip.CompanyName,ulc.EmailId as Clienthr,IRF.JDTiltle,ip.NickName  
from @tbl_RelevantInterviews im
join IRF on IRF.IRFId=im.IRFId
join UserList ul on IRF.RisebirdHr=ul.id
join Client cl on cl.id=IRF.Clientid
left join UserList ulc on ulc.id=IRF.BillingOwner
left join UserList uli on uli.Emailid=im.interviewer
left join InterviewerProfile ip on ip.UserId=uli.id
left join @tbl_InterviewerCompleted icm on icm.IRFId=im.IRFId and icm.interviewer=im.interviewer
left join @tbl_InterviewerRejected ism on ism.IRFId=im.IRFId and ism.interviewer=im.interviewer)
Select * From CTE where rejectionrate>=80
end
END
GO