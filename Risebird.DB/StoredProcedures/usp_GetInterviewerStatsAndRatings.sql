IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerStatsAndRatings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerStatsAndRatings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO      
                      
CREATE PROCEDURE usp_GetInterviewerStatsAndRatings     
                  
@timeNow datetime 

AS          
BEGIN  

declare @yesterdayDate date = cast(dateadd(day, -1, @timeNow) as date)

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
where cast(im.interviewDate as date)>= @yesterdayDate and confirmationStatus = 1


DECLARE @tbl_Interviewer TABLE (                    
    interviewer varchar(200),
	TotalMeetings int
); 

INSERT INTO @tbl_Interviewer    
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im
group by im.interviewer 


DECLARE @tbl_InterviewerSelected TABLE (                    
    interviewer varchar(200),
	TotalSelected int
);

INSERT INTO @tbl_InterviewerSelected  
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.InterviewStatus = 2 and MeetingStatus = 4
group by im.interviewer

DECLARE @tbl_InterviewerBorderLine TABLE (                    
    interviewer varchar(200),
	TotalBorderLine int
);

INSERT INTO @tbl_InterviewerBorderLine  
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.InterviewStatus = 4 and MeetingStatus = 4
group by im.interviewer

DECLARE @tbl_InterviewerRejected TABLE (                    
    interviewer varchar(200),
	TotalRejected int
);

INSERT INTO @tbl_InterviewerRejected  
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.InterviewStatus = 3 and MeetingStatus = 4
group by im.interviewer

DECLARE @tbl_InterviewerProxy TABLE (                    
    interviewer varchar(200),
	TotalProxy int
);

INSERT INTO @tbl_InterviewerProxy  
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.InterviewStatus = 1 and MeetingStatus = 4
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
	TotalPending int
);

INSERT INTO @tbl_InterviewerPending  
select im.interviewer, count(im.id) from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
where ic.MeetingStatus = 2 or (ic.MeetingStatus is null and im.interviewDate < @timeNow)
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


select ti.interviewer as InterviewerEmailId, ti.TotalMeetings, tis.TotalSelected, tir.TotalRejected, tib.TotalBorderLine,
tip.TotalProxy, tid.TotalDropped, tins.TotalSelfNoShow, tinsc.TotalNoShowCandidate, tipen.TotalPending, tipenal.TotalPenalties
from @tbl_Interviewer ti
left join @tbl_InterviewerSelected tis on tis.interviewer = ti.interviewer
left join @tbl_InterviewerRejected tir on tir.interviewer = ti.interviewer
left join @tbl_InterviewerProxy tip on tip.interviewer = ti.interviewer
left join @tbl_InterviewerBorderLine tib on tib.interviewer = ti.interviewer
left join @tbl_InterviewerDropped tid on tid.interviewer = ti.interviewer
left join @tbl_InterviewerNoShow tins on tins.interviewer = ti.interviewer
left join @tbl_InterviewerNoShowCandidate tinsc on tinsc.interviewer = ti.interviewer
left join @tbl_InterviewerPending tipen on tipen.interviewer = ti.interviewer
left join @tbl_InterviewerPenalties tipenal on tipenal.interviewer = ti.interviewer
           
      
END        
  
GO            