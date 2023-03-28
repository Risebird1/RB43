/****** Object:  StoredProcedure  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDateRangeWiseMeetingStatusForClientRoles'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDateRangeWiseMeetingStatusForClientRoles
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
       
CREATE PROCEDURE [dbo].[usp_GetDateRangeWiseMeetingStatusForClientRoles]  
@emailId varchar(100),
@startDateValue datetime, 
@endDateValue datetime,
@clientId int,
@timeNow datetime
AS            
Begin         
 
DECLARE @tbl_RelevantInterviews TABLE (                    
	id int,
	interviewDate datetime,	
	ClientHrContact varchar(200),
	StaffingAgency varchar(200),
	MeetingStatus int,
	InterviewStatus int
); 

INSERT INTO @tbl_RelevantInterviews    
select im.id, im.interviewDate,  
im.ClientHrContact, im.staffingAgency, ic.MeetingStatus, ic.InterviewStatus from InterviewMeetings im 
join IRF on IRF.IRFId = im.IRFId
join Client cl on cl.id=IRF.ClientId
left join InterviewComments ic on ic.InterviewId = im.id
where cast(interviewDate as date) >= cast (@startDateValue as date) and cast(interviewDate as date) <= cast (@endDateValue as date) 
and im.confirmationStatus=1 and cl.id = @clientId 



--client HR tables
DECLARE @tbl_TotalClientHrInterviews TABLE ( 
    ClientHrContact varchar(200),
	TotalMeetings int
);

INSERT INTO @tbl_TotalClientHrInterviews
select ClientHrContact, count(id) from @tbl_RelevantInterviews  
group by ClientHrContact

DECLARE @tbl_InterviewClientHrCompleted TABLE (   
	ClientHrContact varchar(200),
	TotalCompletedMeetings int
); 

INSERT INTO @tbl_InterviewClientHrCompleted    
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
where (MeetingStatus in (4, 6, 7, 8))
group by ClientHrContact


DECLARE @tbl_InterviewClientHrSelected TABLE (
	ClientHrContact varchar(200),
	TotalSelected int
);

INSERT INTO @tbl_InterviewClientHrSelected  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
where (InterviewStatus = 2) and (MeetingStatus = 4 or MeetingStatus = 6)
group by ClientHrContact

DECLARE @tbl_InterviewClientHrBorderLine TABLE (  
	ClientHrContact varchar(200),
	TotalBorderLine int
);

INSERT INTO @tbl_InterviewClientHrBorderLine  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
where (InterviewStatus = 4) and (MeetingStatus = 4 or MeetingStatus = 8)
group by ClientHrContact

DECLARE @tbl_InterviewClientHrRejected TABLE (
	ClientHrContact varchar(200),
	TotalRejected int
);

INSERT INTO @tbl_InterviewClientHrRejected  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
where (InterviewStatus = 3) and (MeetingStatus = 4  or MeetingStatus = 7)
group by ClientHrContact

DECLARE @tbl_InterviewClientHrProxy TABLE (  
	ClientHrContact varchar(200),
	TotalProxy int
);

INSERT INTO @tbl_InterviewClientHrProxy  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
where InterviewStatus = 1 and MeetingStatus = 4
group by ClientHrContact

DECLARE @tbl_InterviewClientHrNoShow TABLE ( 
	ClientHrContact varchar(200),
	TotalNoShow int
);

INSERT INTO @tbl_InterviewClientHrNoShow 
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
where MeetingStatus = 5 or MeetingStatus = 1
group by ClientHrContact

DECLARE @tbl_InterviewClientHrDropped TABLE ( 
	ClientHrContact varchar(200),
	TotalDropped int
);

INSERT INTO @tbl_InterviewClientHrDropped  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
where MeetingStatus = 3
group by ClientHrContact

--StatusPending
DECLARE @tbl_InterviewClientHrStatusPending TABLE (  
	ClientHrContact varchar(200),
	StatusPending int
);

INSERT INTO @tbl_InterviewClientHrStatusPending  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
where (im.interviewDate < @timeNow) and ((MeetingStatus = 4 and InterviewStatus is null) or (MeetingStatus is null) or MeetingStatus = 2)
group by ClientHrContact

DECLARE @tbl_InterviewClientHrScheduled TABLE (  
	ClientHrContact varchar(200),
	TotalScheduled int
);

INSERT INTO @tbl_InterviewClientHrScheduled  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
where (MeetingStatus = 2 and im.interviewDate > @timeNow)--or (ic.MeetingStatus is null and im.interviewDate > @timeNow)
group by ClientHrContact



--staffing agency tables

DECLARE @tbl_TotalStaffingAgencyInterviews TABLE ( 
    StaffingAgency varchar(200),
	TotalMeetings int
);

INSERT INTO @tbl_TotalStaffingAgencyInterviews
select StaffingAgency, count(id) from @tbl_RelevantInterviews  
group by StaffingAgency

DECLARE @tbl_InterviewStaffingAgencyCompleted TABLE (   
	StaffingAgency varchar(200),
	TotalCompletedMeetings int
); 

INSERT INTO @tbl_InterviewStaffingAgencyCompleted    
select im.StaffingAgency, count(im.id) from @tbl_RelevantInterviews im
where (MeetingStatus in (4, 6, 7, 8))
group by StaffingAgency


DECLARE @tbl_InterviewStaffingAgencySelected TABLE (
	StaffingAgency varchar(200),
	TotalSelected int
);

INSERT INTO @tbl_InterviewStaffingAgencySelected  
select im.StaffingAgency, count(im.id) from @tbl_RelevantInterviews im
where (InterviewStatus = 2) and (MeetingStatus = 4 or MeetingStatus = 6)
group by StaffingAgency

DECLARE @tbl_InterviewStaffingAgencyBorderLine TABLE (  
	StaffingAgency varchar(200),
	TotalBorderLine int
);

INSERT INTO @tbl_InterviewStaffingAgencyBorderLine  
select im.StaffingAgency, count(im.id) from @tbl_RelevantInterviews im
where (InterviewStatus = 4) and (MeetingStatus = 4 or MeetingStatus = 8)
group by StaffingAgency

DECLARE @tbl_InterviewStaffingAgencyRejected TABLE (
	StaffingAgency varchar(200),
	TotalRejected int
);

INSERT INTO @tbl_InterviewStaffingAgencyRejected  
select im.StaffingAgency, count(im.id) from @tbl_RelevantInterviews im
where (InterviewStatus = 3) and (MeetingStatus = 4  or MeetingStatus = 7)
group by StaffingAgency

DECLARE @tbl_InterviewStaffingAgencyProxy TABLE (  
	StaffingAgency varchar(200),
	TotalProxy int
);

INSERT INTO @tbl_InterviewStaffingAgencyProxy  
select im.StaffingAgency, count(im.id) from @tbl_RelevantInterviews im
where InterviewStatus = 1 and MeetingStatus = 4
group by StaffingAgency

DECLARE @tbl_InterviewStaffingAgencyNoShow TABLE ( 
	StaffingAgency varchar(200),
	TotalNoShow int
);

INSERT INTO @tbl_InterviewStaffingAgencyNoShow 
select im.StaffingAgency, count(im.id) from @tbl_RelevantInterviews im
where MeetingStatus = 5 or MeetingStatus = 1
group by StaffingAgency

DECLARE @tbl_InterviewStaffingAgencyDropped TABLE ( 
	StaffingAgency varchar(200),
	TotalDropped int
);

INSERT INTO @tbl_InterviewClientHrDropped  
select im.StaffingAgency, count(im.id) from @tbl_RelevantInterviews im
where MeetingStatus = 3
group by StaffingAgency

--StatusPending
DECLARE @tbl_InterviewStaffingAgencyStatusPending TABLE (  
	StaffingAgency varchar(200),
	StatusPending int
);

INSERT INTO @tbl_InterviewStaffingAgencyStatusPending  
select im.StaffingAgency, count(im.id) from @tbl_RelevantInterviews im
where (im.interviewDate < @timeNow) and ((MeetingStatus = 4 and InterviewStatus is null) or (MeetingStatus is null) or MeetingStatus = 2)
group by StaffingAgency

DECLARE @tbl_InterviewStaffingAgencyScheduled TABLE (  
	StaffingAgency varchar(200),
	TotalScheduled int
);

INSERT INTO @tbl_InterviewStaffingAgencyScheduled  
select im.StaffingAgency, count(im.id) from @tbl_RelevantInterviews im
where (MeetingStatus = 2 and im.interviewDate > @timeNow)--or (ic.MeetingStatus is null and im.interviewDate > @timeNow)
group by StaffingAgency



--final counts
select distinct trc.ClientHrContact as EmailId, ti.TotalMeetings, ticom.TotalCompletedMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,
	tip.TotalProxy, tid.TotalDropped, tins.TotalNoShow, tisp.StatusPending, tisch.TotalScheduled as Scheduled,
	ul.MobileNumber, ul.id as UserId
	from @tbl_RelevantInterviews trc 
	left join @tbl_TotalClientHrInterviews  ti on ti.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewClientHrCompleted ticom on ticom.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewClientHrSelected tis on tis.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewClientHrRejected tir on tir.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewClientHrProxy tip on tip.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewClientHrBorderLine tib on tib.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewClientHrDropped tid on tid.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewClientHrNoShow tins on tins.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewClientHrProxy tiproxy on tiproxy.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewClientHrStatusPending tisp on tisp.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewClientHrScheduled tisch on tisch.ClientHrContact = trc.ClientHrContact
	join UserList ul on ul.EmailId = trc.ClientHrContact 
	group by trc.ClientHrContact, ti.TotalMeetings, ticom.TotalCompletedMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,
	tip.TotalProxy, tid.TotalDropped, tins.TotalNoShow, tisp.StatusPending, tisch.TotalScheduled,
	ul.MobileNumber, ul.id
	order by trc.ClientHrContact


select distinct trc.StaffingAgency as EmailId, ti.TotalMeetings, ticom.TotalCompletedMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,
	tip.TotalProxy, tid.TotalDropped, tins.TotalNoShow, tisp.StatusPending, tisch.TotalScheduled as Scheduled,
	ul.MobileNumber, ul.id as UserId
	from @tbl_RelevantInterviews trc 
	left join @tbl_TotalStaffingAgencyInterviews  ti on ti.StaffingAgency = trc.StaffingAgency
	left join @tbl_InterviewStaffingAgencyCompleted ticom on ticom.StaffingAgency = trc.StaffingAgency
	left join @tbl_InterviewStaffingAgencySelected tis on tis.StaffingAgency = trc.StaffingAgency
	left join @tbl_InterviewStaffingAgencyRejected tir on tir.StaffingAgency = trc.StaffingAgency
	left join @tbl_InterviewStaffingAgencyProxy tip on tip.StaffingAgency = trc.StaffingAgency
	left join @tbl_InterviewStaffingAgencyBorderLine tib on tib.StaffingAgency = trc.StaffingAgency
	left join @tbl_InterviewStaffingAgencyDropped tid on tid.StaffingAgency = trc.StaffingAgency
	left join @tbl_InterviewStaffingAgencyNoShow tins on tins.StaffingAgency = trc.StaffingAgency
	left join @tbl_InterviewStaffingAgencyProxy tiproxy on tiproxy.StaffingAgency = trc.StaffingAgency
	left join @tbl_InterviewStaffingAgencyStatusPending tisp on tisp.StaffingAgency = trc.StaffingAgency
	left join @tbl_InterviewStaffingAgencyScheduled tisch on tisch.StaffingAgency = trc.StaffingAgency
	join UserList ul on ul.EmailId = trc.StaffingAgency 
	group by trc.StaffingAgency, ti.TotalMeetings, ticom.TotalCompletedMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,
	tip.TotalProxy, tid.TotalDropped, tins.TotalNoShow, tisp.StatusPending, tisch.TotalScheduled,
	ul.MobileNumber, ul.id
	order by trc.StaffingAgency


End

GO

