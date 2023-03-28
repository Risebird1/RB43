/****** Object:  StoredProcedure  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDateRangeWiseMeetingStatusForClientUserByIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDateRangeWiseMeetingStatusForClientUserByIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
       
CREATE PROCEDURE [dbo].[usp_GetDateRangeWiseMeetingStatusForClientByIRF]  
@emailId varchar(100),
@startDateValue datetime, 
@endDateValue datetime,
@clientId int,
@timeNow datetime,
@clientUserEmailId varchar(100)
AS            
Begin         
 
DECLARE @tbl_RelevantInterviews TABLE (                    
	id int,
	interviewDate datetime,
	IRFId varchar(30),
	IRFnSkill varchar(max),
	ClientUserEmailId varchar(100)
	--ClientHrContact varchar(200),
	--StaffingAgency varchar(200)
); 


INSERT INTO @tbl_RelevantInterviews    
select im.id, im.interviewDate, im.IRFId,  (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,
@clientUserEmailId from InterviewMeetings im --im.ClientHrContact, im.staffingAgency
join IRF on IRF.IRFId = im.IRFId
join Client cl on cl.id=IRF.ClientId
where cast(interviewDate as date) >= cast (@startDateValue as date) and cast(interviewDate as date) <= cast (@endDateValue as date) 
and im.confirmationStatus=1 and cl.id = @clientId 
and (ClientHrContact = @clientUserEmailId or staffingAgency = @clientUserEmailId)


DECLARE @tbl_TotalInterviews TABLE ( 
    EmailId varchar(200),
	IRFId varchar(30),
	TotalMeetings int
);

INSERT INTO @tbl_TotalInterviews
select ClientUserEmailId, IRFId, count(id) from @tbl_RelevantInterviews  
group by ClientUserEmailId, IRFId

DECLARE @tbl_InterviewCompleted TABLE (   
	EmailId varchar(200),
	IRFId varchar(30),
	TotalCompletedMeetings int
); 

INSERT INTO @tbl_InterviewCompleted    
select im.ClientUserEmailId, im.IRFId, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (ic.MeetingStatus in (4, 6, 7, 8))
group by ClientUserEmailId, IRFId


DECLARE @tbl_InterviewSelected TABLE (
	EmailId varchar(200),
	IRFId varchar(30),
	TotalSelected int
);

INSERT INTO @tbl_InterviewSelected  
select im.ClientUserEmailId, im.IRFId, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (ic.InterviewStatus = 2) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 6)
group by ClientUserEmailId, IRFId

DECLARE @tbl_InterviewBorderLine TABLE (  
	EmailId varchar(200),
	IRFId varchar(30),
	TotalBorderLine int
);

INSERT INTO @tbl_InterviewBorderLine  
select im.ClientUserEmailId, im.IRFId, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (ic.InterviewStatus = 4) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 8)
group by ClientUserEmailId, IRFId

DECLARE @tbl_InterviewRejected TABLE (
	EmailId varchar(200),
	IRFId varchar(30),
	TotalRejected int
);

INSERT INTO @tbl_InterviewRejected  
select im.ClientUserEmailId, im.IRFId, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (ic.InterviewStatus = 3) and (ic.MeetingStatus = 4  or ic.MeetingStatus = 7)
group by ClientUserEmailId, IRFId

DECLARE @tbl_InterviewProxy TABLE (  
	EmailId varchar(200),
	IRFId varchar(30),
	TotalProxy int
);

INSERT INTO @tbl_InterviewProxy  
select im.ClientUserEmailId, im.IRFId, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.InterviewStatus = 1 and ic.MeetingStatus = 4
group by ClientUserEmailId, IRFId

DECLARE @tbl_InterviewNoShow TABLE ( 
	EmailId varchar(200),
	IRFId varchar(30),
	TotalNoShow int
);

INSERT INTO @tbl_InterviewNoShow 
select im.ClientUserEmailId, im.IRFId, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.MeetingStatus = 5 or ic.MeetingStatus = 1
group by ClientUserEmailId, IRFId

DECLARE @tbl_InterviewDropped TABLE ( 
	EmailId varchar(200),
	IRFId varchar(30),
	TotalDropped int
);

INSERT INTO @tbl_InterviewDropped  
select im.ClientUserEmailId, im.IRFId, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.MeetingStatus = 3
group by ClientUserEmailId, IRFId

--StatusPending
DECLARE @tbl_InterviewStatusPending TABLE (  
	EmailId varchar(200),
	IRFId varchar(30),
	StatusPending int
);

INSERT INTO @tbl_InterviewStatusPending  
select im.ClientUserEmailId, im.IRFId, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (im.interviewDate < @timeNow) and ((ic.MeetingStatus = 4 and ic.InterviewStatus is null) or (ic.MeetingStatus is null) or ic.MeetingStatus = 2)
group by ClientUserEmailId, IRFId

DECLARE @tbl_InterviewScheduled TABLE (  
	EmailId varchar(200),
	IRFId varchar(30),
	TotalScheduled int
);

INSERT INTO @tbl_InterviewScheduled  
select im.ClientUserEmailId, im.IRFId, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (ic.MeetingStatus = 2 and im.interviewDate > @timeNow)--or (ic.MeetingStatus is null and im.interviewDate > @timeNow)
group by ClientUserEmailId, IRFId


--final counts
select distinct trc.ClientUserEmailId as EmailId, trc.IRFnSkill, ti.TotalMeetings, ticom.TotalCompletedMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,
	tip.TotalProxy, tid.TotalDropped, tins.TotalNoShow, tisp.StatusPending, tisch.TotalScheduled as Scheduled,
	ul.MobileNumber, ul.id as UserId
	from @tbl_RelevantInterviews trc 
	left join @tbl_TotalInterviews  ti on ti.EmailId = trc.ClientUserEmailId
	left join @tbl_InterviewCompleted ticom on ticom.EmailId = trc.ClientUserEmailId
	left join @tbl_InterviewSelected tis on tis.EmailId = trc.ClientUserEmailId
	left join @tbl_InterviewRejected tir on tir.EmailId = trc.ClientUserEmailId
	left join @tbl_InterviewProxy tip on tip.EmailId = trc.ClientUserEmailId
	left join @tbl_InterviewBorderLine tib on tib.EmailId = trc.ClientUserEmailId
	left join @tbl_InterviewDropped tid on tid.EmailId = trc.ClientUserEmailId
	left join @tbl_InterviewNoShow tins on tins.EmailId = trc.ClientUserEmailId
	left join @tbl_InterviewProxy tiproxy on tiproxy.EmailId = trc.ClientUserEmailId
	left join @tbl_InterviewStatusPending tisp on tisp.EmailId = trc.ClientUserEmailId
	left join @tbl_InterviewScheduled tisch on tisch.EmailId = trc.ClientUserEmailId
	join UserList ul on ul.EmailId = trc.ClientUserEmailId order by trc.ClientUserEmailId




End

GO

