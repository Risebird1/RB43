/****** Object:  StoredProcedure [dbo].[usp_GetStaffingAgencyStats]    Script Date: 3/5/2023 1:51:05 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetStaffingAgencyStats'))
BEGIN
    DROP PROCEDURE dbo.usp_GetStaffingAgencyStats
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO 
                      
Create PROCEDURE [dbo].[usp_GetStaffingAgencyStats]     
                    
@timeNow datetime,  
@irfId int = null  
  
AS            
BEGIN    
  
declare @yesterdayDate date = cast(dateadd(day, -1, @timeNow) as date)   
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
 staffingAgency varchar(200)  
);   
  
INSERT INTO @tbl_RelevantInterviews      
select im.id, im.interviewDate, im.staffingAgency from @tbl_RelevantCandidates trc --.ProfileSource  
join InterviewMeetings im on im.candidateEmail = trc.CandidateEmail and im.IRFId = trc.IRFName 
and im.staffingAgency is not null and im.staffingAgency != ''
and ((cast(im.interviewDate as date)>= @yesterdayDate and @irfId is null) or im.IRFId = @IRFName) -- im.staffingAgency = trc.ProfileSource  and  
where im.confirmationStatus = 1  
  
  
DECLARE @tbl_TotalInterviews TABLE (   
    staffingAgency varchar(200),  
 TotalMeetings int  
);  
  
INSERT INTO @tbl_TotalInterviews  
select staffingAgency, count(id) from @tbl_RelevantInterviews    
group by staffingAgency  
  
DECLARE @tbl_InterviewCompleted TABLE (     
 staffingAgency varchar(200),  
 TotalCompletedMeetings int  
);   
  
INSERT INTO @tbl_InterviewCompleted      
select im.staffingAgency, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.MeetingStatus in (4, 6, 7, 8))  
group by staffingAgency  
  
  
DECLARE @tbl_InterviewSelected TABLE (  
 staffingAgency varchar(200),  
 TotalSelected int  
);  
  
INSERT INTO @tbl_InterviewSelected    
select im.staffingAgency, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.InterviewStatus = 2) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 6)  
group by staffingAgency  
  
DECLARE @tbl_InterviewBorderLine TABLE (    
 staffingAgency varchar(200),  
 TotalBorderLine int  
);  
  
INSERT INTO @tbl_InterviewBorderLine    
select im.staffingAgency, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.InterviewStatus = 4) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 8)  
group by staffingAgency  
  
DECLARE @tbl_InterviewRejected TABLE (  
 staffingAgency varchar(200),  
 TotalRejected int  
);  
  
INSERT INTO @tbl_InterviewRejected    
select im.staffingAgency, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.InterviewStatus = 3) and (ic.MeetingStatus = 4  or ic.MeetingStatus = 7)  
group by staffingAgency  
  
DECLARE @tbl_InterviewProxy TABLE (    
 staffingAgency varchar(200),  
 TotalProxy int  
);  
  
INSERT INTO @tbl_InterviewProxy    
select im.staffingAgency, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where ic.InterviewStatus = 1 and ic.MeetingStatus = 4  
group by staffingAgency  
  
DECLARE @tbl_InterviewNoShowInterviewer TABLE (   
 staffingAgency varchar(200),  
 TotalInterviewerNoShow int  
);  
  
INSERT INTO @tbl_InterviewNoShowInterviewer    
select im.staffingAgency, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where ic.MeetingStatus = 5  
group by staffingAgency  
  
  
DECLARE @tbl_InterviewCandidateNoShow TABLE (  
 staffingAgency varchar(200),  
 TotalNoShowCandidate int  
);  
  
INSERT INTO @tbl_InterviewCandidateNoShow    
select im.staffingAgency, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where ic.MeetingStatus = 1  
group by staffingAgency  
  
  
DECLARE @tbl_InterviewDropped TABLE (   
 staffingAgency varchar(200),  
 TotalDropped int  
);  
  
INSERT INTO @tbl_InterviewDropped    
select im.staffingAgency, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where ic.MeetingStatus = 3  
group by staffingAgency  
  
--StatusPending  
DECLARE @tbl_InterviewStatusPending TABLE (    
 staffingAgency varchar(200),  
 StatusPending int  
);  
  
INSERT INTO @tbl_InterviewStatusPending    
select im.staffingAgency, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (im.interviewDate < @timeNow) and ((ic.MeetingStatus = 4 and ic.InterviewStatus is null) or (ic.MeetingStatus is null) or ic.MeetingStatus = 2)  
group by staffingAgency  
  
DECLARE @tbl_InterviewScheduled TABLE (    
 staffingAgency varchar(200),  
 TotalScheduled int  
);  
  
INSERT INTO @tbl_InterviewScheduled    
select im.staffingAgency, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.MeetingStatus = 2 and im.interviewDate > @timeNow)--or (ic.MeetingStatus is null and im.interviewDate > @timeNow)  
group by staffingAgency  
  
  
 select distinct trc.staffingAgency as EmailId, ti.TotalMeetings, ticom.TotalCompletedMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,  
 tip.TotalProxy, tid.TotalDropped, tins.TotalInterviewerNoShow, tinsc.TotalNoShowCandidate, tisp.StatusPending, tisch.TotalScheduled as Scheduled,  
 ul.MobileNumber, ul.id as UserId  
 from @tbl_RelevantInterviews trc   
 left join @tbl_TotalInterviews  ti on ti.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewCompleted ticom on ticom.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewSelected tis on tis.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewRejected tir on tir.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewProxy tip on tip.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewBorderLine tib on tib.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewDropped tid on tid.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewNoShowInterviewer tins on tins.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewCandidateNoShow tinsc on tinsc.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewProxy tiproxy on tiproxy.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewStatusPending tisp on tisp.staffingAgency = trc.staffingAgency  
 left join @tbl_InterviewScheduled tisch on tisch.staffingAgency = trc.staffingAgency  
 left join UserList ul on ul.EmailId = trc.staffingAgency  
   
        
END          
             
  
GO


