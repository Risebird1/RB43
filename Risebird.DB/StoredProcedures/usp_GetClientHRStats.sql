
/****** Object:  StoredProcedure [dbo].[usp_GetClientHRStats]   ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientHRStats'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientHRStats
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO 
                      
CREATE PROCEDURE [dbo].[usp_GetClientHRStats]   
                  
@timeNow datetime,
@irfId int = null

AS          
BEGIN  

declare @IRFName varchar(30)
if(@irfId is not null)
begin
	set @IRFName = (select IRFId from IRF where id = @irfId)
end


DECLARE @tbl_RelevantCandidates TABLE (                    
	CandidateUserId int,
	CandidateEmail varchar(200),
	IRFId int,
	IRFName varchar(30)	
)

INSERT INTO @tbl_RelevantCandidates 
select cm.UserId, ul.EmailId as CandidateEmail, ca.IRFId, irf.IRFId as IRFName
from Candidate ca join CandidatesMaster cm
on cm.id = ca.CandidateMasterId
join UserList ul on ul.id = cm.UserId
join IRF on irf.id = ca.IRFId
where (@irfId is null or ca.IRFId = @irfId)
group by cm.UserId, ul.EmailId, ca.IRFId, irf.IRFId


DECLARE @tbl_RelevantInterviews TABLE (                    
	id int,
	interviewDate datetime,
	ClientHrContact varchar(200)
); 

INSERT INTO @tbl_RelevantInterviews    
select im.id, im.interviewDate, im.ClientHrContact from @tbl_RelevantCandidates trc 
join InterviewMeetings im on im.candidateEmail = trc.CandidateEmail and im.IRFId = trc.IRFName 
and  (@irfId is null or im.IRFId = @IRFName) 
where im.confirmationStatus = 1


DECLARE @tbl_TotalInterviews TABLE ( 
    staffingAgency varchar(200),
	TotalMeetings int
);

INSERT INTO @tbl_TotalInterviews
select ClientHrContact, count(id) from @tbl_RelevantInterviews  
group by ClientHrContact

DECLARE @tbl_InterviewCompleted TABLE (   
	ClientHrContact varchar(200),
	TotalCompletedMeetings int
); 

INSERT INTO @tbl_InterviewCompleted    
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (ic.MeetingStatus in (4, 6, 7, 8))
group by ClientHrContact


DECLARE @tbl_InterviewSelected TABLE (
	ClientHrContact varchar(200),
	TotalSelected int
);

INSERT INTO @tbl_InterviewSelected  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (ic.InterviewStatus = 2) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 6)
group by ClientHrContact

DECLARE @tbl_InterviewBorderLine TABLE (  
	ClientHrContact varchar(200),
	TotalBorderLine int
);

INSERT INTO @tbl_InterviewBorderLine  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (ic.InterviewStatus = 4) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 8)
group by ClientHrContact

DECLARE @tbl_InterviewRejected TABLE (
	ClientHrContact varchar(200),
	TotalRejected int
);

INSERT INTO @tbl_InterviewRejected  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (ic.InterviewStatus = 3) and (ic.MeetingStatus = 4  or ic.MeetingStatus = 7)
group by ClientHrContact

DECLARE @tbl_InterviewProxy TABLE (  
	ClientHrContact varchar(200),
	TotalProxy int
);

INSERT INTO @tbl_InterviewProxy  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.InterviewStatus = 1 and ic.MeetingStatus = 4
group by ClientHrContact

DECLARE @tbl_InterviewNoShowInterviewer TABLE ( 
	ClientHrContact varchar(200),
	TotalInterviewerNoShow int
);

INSERT INTO @tbl_InterviewNoShowInterviewer  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.MeetingStatus = 5
group by ClientHrContact


DECLARE @tbl_InterviewCandidateNoShow TABLE (
	ClientHrContact varchar(200),
	TotalNoShowCandidate int
);

INSERT INTO @tbl_InterviewCandidateNoShow  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.MeetingStatus = 1
group by ClientHrContact


DECLARE @tbl_InterviewDropped TABLE ( 
	ClientHrContact varchar(200),
	TotalDropped int
);

INSERT INTO @tbl_InterviewDropped  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.MeetingStatus = 3
group by ClientHrContact

--StatusPending
DECLARE @tbl_InterviewStatusPending TABLE (  
	ClientHrContact varchar(200),
	StatusPending int
);

INSERT INTO @tbl_InterviewStatusPending  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (im.interviewDate < @timeNow) and ((ic.MeetingStatus = 4 and ic.InterviewStatus is null) or (ic.MeetingStatus is null) or ic.MeetingStatus = 2)
group by ClientHrContact

DECLARE @tbl_InterviewScheduled TABLE (  
	ClientHrContact varchar(200),
	TotalScheduled int
);

INSERT INTO @tbl_InterviewScheduled  
select im.ClientHrContact, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where (ic.MeetingStatus = 2 and im.interviewDate > @timeNow)--or (ic.MeetingStatus is null and im.interviewDate > @timeNow)
group by ClientHrContact


	select distinct trc.ClientHrContact as EmailId, ti.TotalMeetings, ticom.TotalCompletedMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,
	tip.TotalProxy, tid.TotalDropped, tins.TotalInterviewerNoShow, tinsc.TotalNoShowCandidate, tisp.StatusPending, tisch.TotalScheduled as Scheduled,
	ul.MobileNumber, ul.id as UserId
	from @tbl_RelevantInterviews trc 
	left join @tbl_TotalInterviews  ti on ti.staffingAgency = trc.ClientHrContact
	left join @tbl_InterviewCompleted ticom on ticom.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewSelected tis on tis.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewRejected tir on tir.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewProxy tip on tip.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewBorderLine tib on tib.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewDropped tid on tid.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewNoShowInterviewer tins on tins.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewCandidateNoShow tinsc on tinsc.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewProxy tiproxy on tiproxy.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewStatusPending tisp on tisp.ClientHrContact = trc.ClientHrContact
	left join @tbl_InterviewScheduled tisch on tisch.ClientHrContact = trc.ClientHrContact
	join UserList ul on ul.EmailId = trc.ClientHrContact
	
      
END        
  
GO


