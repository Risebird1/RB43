IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerStatsAndRatings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerStatsAndRatings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO      
                      
CREATE PROCEDURE usp_GetInterviewerStatsAndRatings                              
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
  
DECLARE @tbl_RelevantInterviews TABLE (                      
 id int,  
 interviewDate date,  
 interviewer varchar(200),  
 interviewerDateTime datetime,  
 UserId int  
)  
  
INSERT INTO @tbl_RelevantInterviews      
select im.id, cast(interviewDate as date), interviewer, interviewDate, ul.id  from InterviewMeetings im  
join UserList ul on ul.EmailId = im.interviewer  
where ((cast(im.interviewDate as date)>= @yesterdayDate and @irfId is null) or @irfId is not null) and confirmationStatus = 1 and (@IRFName is null or im.IRFId = @IRFName)  
  
  
DECLARE @tbl_Interviewer TABLE (                      
    interviewer varchar(200),  
 TotalMeetings int  
);   
  
INSERT INTO @tbl_Interviewer      
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
group by im.interviewer   
  
DECLARE @tbl_InterviewerCompleted TABLE (                      
    interviewer varchar(200),  
 TotalCompletedMeetings int  
);   
  
INSERT INTO @tbl_InterviewerCompleted      
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.MeetingStatus in (4, 6, 7, 8))  
group by im.interviewer  
  
  
DECLARE @tbl_InterviewerSelected TABLE (                      
    interviewer varchar(200),  
 TotalSelected int  
);  
  
INSERT INTO @tbl_InterviewerSelected    
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.InterviewStatus = 2) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 6)  
group by im.interviewer  
  
DECLARE @tbl_InterviewerBorderLine TABLE (                      
    interviewer varchar(200),  
 TotalBorderLine int  
);  
  
INSERT INTO @tbl_InterviewerBorderLine    
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.InterviewStatus = 4) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 8)  
group by im.interviewer  
  
DECLARE @tbl_InterviewerRejected TABLE (                      
    interviewer varchar(200),  
 TotalRejected int  
);  
  
INSERT INTO @tbl_InterviewerRejected    
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.InterviewStatus = 3) and (ic.MeetingStatus = 4  or ic.MeetingStatus = 7)  
group by im.interviewer  
  
DECLARE @tbl_InterviewerProxy TABLE (                      
    interviewer varchar(200),  
 TotalProxy int  
);  
  
INSERT INTO @tbl_InterviewerProxy    
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where ic.InterviewStatus = 1 and ic.MeetingStatus = 4  
group by im.interviewer  
  
  
DECLARE @tbl_InterviewerNoShow TABLE (                      
    interviewer varchar(200),  
 TotalSelfNoShow int  
);  
  
INSERT INTO @tbl_InterviewerNoShow    
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where ic.MeetingStatus = 5  
group by im.interviewer  
  
  
DECLARE @tbl_InterviewerNoShowCandidate TABLE (                      
    interviewer varchar(200),  
 TotalNoShowCandidate int  
);  
  
INSERT INTO @tbl_InterviewerNoShowCandidate    
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where ic.MeetingStatus = 1  
group by im.interviewer  
  
DECLARE @tbl_InterviewerDropped TABLE (                      
    interviewer varchar(200),  
 TotalDropped int  
);  
  
INSERT INTO @tbl_InterviewerDropped    
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where ic.MeetingStatus = 3  
group by im.interviewer  
  
DECLARE @tbl_InterviewerPending TABLE (                      
    interviewer varchar(200),  
 StatusPending int  
);  
  
INSERT INTO @tbl_InterviewerPending    
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (im.interviewerDateTime < @timeNow) and ((ic.MeetingStatus = 4 and ic.InterviewStatus is null) or (ic.MeetingStatus is null) or ic.MeetingStatus = 2)
group by im.interviewer  
  
DECLARE @tbl_InterviewerScheduled TABLE (                      
    interviewer varchar(200),  
 Scheduled int  
);  
  
INSERT INTO @tbl_InterviewerScheduled    
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im  
left join InterviewComments ic on ic.InterviewId = im.id  
where (ic.MeetingStatus = 2 and im.interviewerDateTime > @timeNow)  
group by im.interviewer  
  
DECLARE @tbl_InterviewerPenalties TABLE (   
 interviewer varchar(200),  
 TotalPenalties int  
);  
  
INSERT INTO @tbl_InterviewerPenalties    
select im.interviewer, count(im.interviewer) from @tbl_Interviewer im  
left join UserList ul on  ul.EmailId = im.interviewer  
left join InterviewPoints ipt on ipt.userid = ul.id  
where ipt.Points < 0  
group by im.interviewer  
  
  
DECLARE @tbl_InterviewerClientRatings TABLE (   
 interviewer varchar(200),  
 AverageRating int  
);  
  
INSERT INTO @tbl_InterviewerClientRatings    
  
select  ti.interviewer, AVG(ir.StarRating) from @tbl_Interviewer ti   
left join InterviewMeetings im on im.interviewer = ti.interviewer  
left join InterviewRatings ir on ir.Interviewid = im.id  
where ir.Comment not like '%Redflags raised%'  
group by ti.interviewer  
  
  
  
  
if(@irfId is not null)  
Begin  
 select  ul.EmailId as InterviewerEmailId, ti.TotalMeetings, ticom.TotalCompletedMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,  
 tip.TotalProxy, tid.TotalDropped, tins.TotalSelfNoShow, tinsc.TotalNoShowCandidate, tipen.StatusPending, tisch.Scheduled,  
 tipenal.TotalPenalties, ipr.ResumeLink, ul.MobileNumber as InterviewerPhone, ul.id as UserId, tic.AverageRating as AverageClientRating  
 from TransInterviewer tin   
 join UserList ul on ul.id = tin.UserId  
 join InterviewerProfile ipr on ipr.UserId = ul.id  
 left join @tbl_Interviewer ti on ti.interviewer = ul.EmailId  
 left join @tbl_InterviewerCompleted ticom on ticom.interviewer = ti.interviewer  
 left join @tbl_InterviewerSelected tis on tis.interviewer = ti.interviewer  
 left join @tbl_InterviewerRejected tir on tir.interviewer = ti.interviewer  
 left join @tbl_InterviewerProxy tip on tip.interviewer = ti.interviewer  
 left join @tbl_InterviewerBorderLine tib on tib.interviewer = ti.interviewer  
 left join @tbl_InterviewerDropped tid on tid.interviewer = ti.interviewer  
 left join @tbl_InterviewerNoShow tins on tins.interviewer = ti.interviewer  
 left join @tbl_InterviewerNoShowCandidate tinsc on tinsc.interviewer = ti.interviewer  
 left join @tbl_InterviewerPending tipen on tipen.interviewer = ti.interviewer  
 left join @tbl_InterviewerScheduled tisch on tisch.interviewer = ti.interviewer  
 left join @tbl_InterviewerPenalties tipenal on tipenal.interviewer = ti.interviewer  
 left join @tbl_InterviewerClientRatings tic on tic.interviewer = ti.interviewer  
 where tin.IRFId = @irfId  
End  
else  
Begin  
 select  ti.interviewer as InterviewerEmailId, ti.TotalMeetings, ticom.TotalCompletedMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,  
 tip.TotalProxy, tid.TotalDropped, tins.TotalSelfNoShow, tinsc.TotalNoShowCandidate, tipen.StatusPending, tisch.Scheduled,  
 tipenal.TotalPenalties, ipr.ResumeLink, ul.MobileNumber as InterviewerPhone, ul.id as UserId, tic.AverageRating as AverageClientRating  
 from @tbl_Interviewer ti  
 left join @tbl_InterviewerCompleted ticom on ticom.interviewer = ti.interviewer  
 left join @tbl_InterviewerSelected tis on tis.interviewer = ti.interviewer  
 left join @tbl_InterviewerRejected tir on tir.interviewer = ti.interviewer  
 left join @tbl_InterviewerProxy tip on tip.interviewer = ti.interviewer  
 left join @tbl_InterviewerBorderLine tib on tib.interviewer = ti.interviewer  
 left join @tbl_InterviewerDropped tid on tid.interviewer = ti.interviewer  
 left join @tbl_InterviewerNoShow tins on tins.interviewer = ti.interviewer  
 left join @tbl_InterviewerNoShowCandidate tinsc on tinsc.interviewer = ti.interviewer  
 left join @tbl_InterviewerPending tipen on tipen.interviewer = ti.interviewer  
 left join @tbl_InterviewerScheduled tisch on tisch.interviewer = ti.interviewer  
 left join @tbl_InterviewerPenalties tipenal on tipenal.interviewer = ti.interviewer  
 left join @tbl_InterviewerClientRatings tic on tic.interviewer = ti.interviewer  
 join UserList ul on ul.EmailId = ti.interviewer  
 join InterviewerProfile ipr on ipr.UserId = ul.id  
End  
  
  
  
  
--select * into  #Temptable from (select  ti.interviewer as InterviewerEmailId, ti.TotalMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,  
--tip.TotalProxy, tid.TotalDropped, tins.TotalSelfNoShow, tinsc.TotalNoShowCandidate, tipen.StatusPending, tisch.Scheduled,  
--tipenal.TotalPenalties, ipr.ResumeLink, ul.MobileNumber as InterviewerPhone, ul.id as UserId  
--from @tbl_Interviewer ti  
--left join @tbl_InterviewerSelected tis on tis.interviewer = ti.interviewer  
--left join @tbl_InterviewerRejected tir on tir.interviewer = ti.interviewer  
--left join @tbl_InterviewerProxy tip on tip.interviewer = ti.interviewer  
--left join @tbl_InterviewerBorderLine tib on tib.interviewer = ti.interviewer  
--left join @tbl_InterviewerDropped tid on tid.interviewer = ti.interviewer  
--left join @tbl_InterviewerNoShow tins on tins.interviewer = ti.interviewer  
--left join @tbl_InterviewerNoShowCandidate tinsc on tinsc.interviewer = ti.interviewer  
--left join @tbl_InterviewerPending tipen on tipen.interviewer = ti.interviewer  
--left join @tbl_InterviewerScheduled tisch on tisch.interviewer = ti.interviewer  
--left join @tbl_InterviewerPenalties tipenal on tipenal.interviewer = ti.interviewer  
--join UserList ul on ul.EmailId = ti.interviewer  
--join InterviewerProfile ipr on ipr.UserId = ul.id) a  
  
--if(@irfId is not null)  
--Begin  
-- select tt.* from TransInterviewer tio   
-- left join #Temptable tt on tio.UserId = tt.UserId  
-- where tio.IsActive=1 and tio.IRFId = @irfId  
--End  
--else  
--Begin  
-- select * from #Temptable  
--End  
             
        
END 

                        
       
    