/****** Object:  StoredProcedure  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDateRangeWiseMeetingStatusForClientUserByIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDateRangeWiseMeetingStatusForClientUserByIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
       
CREATE PROCEDURE [dbo].[usp_GetDateRangeWiseMeetingStatusForClientUserByIRF]  
@emailId varchar(100),
@startDateValue datetime, 
@endDateValue datetime,
@clientId int,
@timeNow datetime,
@clientUserEmailId varchar(100),
@role varchar(20)
AS            
Begin         
 
DECLARE @tbl_RelevantInterviews TABLE (                    
	id int,
	interviewDate datetime,
	IRFId varchar(30),
	IRFnSkill varchar(max),
	ClientUserEmailId varchar(100),
	MeetingStatus int,
	InterviewStatus int,
	irfIntId int
	--ClientHrContact varchar(200),
	--StaffingAgency varchar(200)
); 


INSERT INTO @tbl_RelevantInterviews    
select im.id, im.interviewDate, im.IRFId,  (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,
@clientUserEmailId, ic.MeetingStatus, ic.InterviewStatus, irf.id from InterviewMeetings im --im.ClientHrContact, im.staffingAgency
join IRF on IRF.IRFId = im.IRFId
join Client cl on cl.id=IRF.ClientId
left join InterviewComments ic on ic.InterviewId = im.id
where cast(interviewDate as date) >= cast (@startDateValue as date) and cast(interviewDate as date) <= cast (@endDateValue as date) 
and im.confirmationStatus=1 and cl.id = @clientId 
and ((ClientHrContact = @clientUserEmailId and @role = 'ClientHR') or (staffingAgency = @clientUserEmailId and @role= 'Staffing Agency'))


DECLARE @tbl_TotalInterviews TABLE ( 
	IRFId varchar(30),
	TotalMeetings int
);

INSERT INTO @tbl_TotalInterviews
select IRFId, count(id) from @tbl_RelevantInterviews  
group by IRFId

DECLARE @tbl_InterviewCompleted TABLE (   
	IRFId varchar(30),
	TotalCompletedMeetings int
); 

INSERT INTO @tbl_InterviewCompleted    
select im.IRFId, count(im.id) from @tbl_RelevantInterviews im
where (MeetingStatus in (4, 6, 7, 8))
group by IRFId


DECLARE @tbl_InterviewSelected TABLE (
	IRFId varchar(30),
	TotalSelected int
);

INSERT INTO @tbl_InterviewSelected  
select im.IRFId, count(im.id) from @tbl_RelevantInterviews im
where (InterviewStatus = 2) and (MeetingStatus = 4 or MeetingStatus = 6)
group by IRFId

DECLARE @tbl_InterviewBorderLine TABLE (  
	IRFId varchar(30),
	TotalBorderLine int
);

INSERT INTO @tbl_InterviewBorderLine  
select im.IRFId, count(im.id) from @tbl_RelevantInterviews im
where (InterviewStatus = 4) and (MeetingStatus = 4 or MeetingStatus = 8)
group by IRFId

DECLARE @tbl_InterviewRejected TABLE (
	IRFId varchar(30),
	TotalRejected int
);

INSERT INTO @tbl_InterviewRejected  
select im.IRFId, count(im.id) from @tbl_RelevantInterviews im
where (InterviewStatus = 3) and (MeetingStatus = 4  or MeetingStatus = 7)
group by IRFId

DECLARE @tbl_InterviewProxy TABLE (  
	IRFId varchar(30),
	TotalProxy int
);

INSERT INTO @tbl_InterviewProxy  
select im.IRFId, count(im.id) from @tbl_RelevantInterviews im
where InterviewStatus = 1 and MeetingStatus = 4
group by IRFId

DECLARE @tbl_InterviewNoShow TABLE ( 
	IRFId varchar(30),
	TotalNoShow int
);

INSERT INTO @tbl_InterviewNoShow 
select im.IRFId, count(im.id) from @tbl_RelevantInterviews im
where MeetingStatus = 5 or MeetingStatus = 1
group by IRFId

DECLARE @tbl_InterviewDropped TABLE ( 
	IRFId varchar(30),
	TotalDropped int
);

INSERT INTO @tbl_InterviewDropped  
select im.IRFId, count(im.id) from @tbl_RelevantInterviews im
where MeetingStatus = 3
group by IRFId

--StatusPending
DECLARE @tbl_InterviewStatusPending TABLE (  
	IRFId varchar(30),
	StatusPending int
);

INSERT INTO @tbl_InterviewStatusPending  
select im.IRFId, count(im.id) from @tbl_RelevantInterviews im
where (im.interviewDate < @timeNow) and ((MeetingStatus = 4 and InterviewStatus is null) or (MeetingStatus is null) or MeetingStatus = 2)
group by IRFId

DECLARE @tbl_InterviewScheduled TABLE (  
	IRFId varchar(30),
	TotalScheduled int
);

INSERT INTO @tbl_InterviewScheduled  
select im.IRFId, count(im.id) from @tbl_RelevantInterviews im
where (MeetingStatus = 2 and im.interviewDate > @timeNow)--or (ic.MeetingStatus is null and im.interviewDate > @timeNow)
group by IRFId


--final counts
select distinct trc.ClientUserEmailId as EmailId, trc.IRFnSkill, ti.TotalMeetings, ticom.TotalCompletedMeetings, 
	tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,
	tip.TotalProxy, tid.TotalDropped, tins.TotalNoShow, tisp.StatusPending, tisch.TotalScheduled as Scheduled,
	ul.MobileNumber, ul.id as UserId, trc.IRFId, trc.irfIntId 
	from @tbl_RelevantInterviews trc 
	left join @tbl_TotalInterviews  ti on ti.IRFId = trc.IRFId
	left join @tbl_InterviewCompleted ticom on  ticom.IRFId = trc.IRFId
	left join @tbl_InterviewSelected tis on tis.IRFId = trc.IRFId
	left join @tbl_InterviewRejected tir on tir.IRFId = trc.IRFId
	left join @tbl_InterviewProxy tip on  tip.IRFId = trc.IRFId
	left join @tbl_InterviewBorderLine tib on  tib.IRFId = trc.IRFId
	left join @tbl_InterviewDropped tid on tid.IRFId = trc.IRFId
	left join @tbl_InterviewNoShow tins on tins.IRFId = trc.IRFId
	left join @tbl_InterviewProxy tiproxy on  tiproxy.IRFId = trc.IRFId
	left join @tbl_InterviewStatusPending tisp on tisp.IRFId = trc.IRFId
	left join @tbl_InterviewScheduled tisch on tisch.IRFId = trc.IRFId
	join UserList ul on ul.EmailId = trc.ClientUserEmailId
	group by trc.ClientUserEmailId, trc.IRFnSkill, ti.TotalMeetings, ticom.TotalCompletedMeetings, 
	tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,
	tip.TotalProxy, tid.TotalDropped, tins.TotalNoShow, tisp.StatusPending, tisch.TotalScheduled,
	ul.MobileNumber, ul.id, trc.IRFId, trc.irfIntId
	order by trc.ClientUserEmailId




End

GO

