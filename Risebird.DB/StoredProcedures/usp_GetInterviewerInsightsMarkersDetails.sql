IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerInsightsMarkersDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerInsightsMarkersDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetInterviewerInsightsMarkersDetails
@Emailid varchar(200),
@type varchar(50)
AS
BEGIN
if(@type Like 'Redo%')
begin
Select ri.id,ri.Comments,ri.CreatedBy,ri.Createdon,ri.Inappropriate as MeetingStatus From RedoInterviews ri join InterviewMeetings im on im.id=ri.interviewid where im.interviewer=@Emailid and im.RedoInterview=1
end
else if(@type Like 'Suspicious%')
begin
Select si.id,si.Comment as Comments ,si.CreatedBy,si.Createddate as Createdon From SuspiciousInterviewer si join UserList ul on ul.id=si.UserId where SuspiciousStatus=1 and ul.EmailId=@Emailid    
end
else if(@type Like 'Client Rating%')
begin
Select cr.id,cr.Comment as Comments,cr.CreatedBy,cr.CreatedAt as Createdon,cr.Rating as MeetingStatus  From ClientCommentsForReport cr join Interviewmeetings im on im.id=cr.Interviewid where im.interviewer=@Emailid    
end
else if(@type Like 'Candidate Rating%')
begin
Select  cr.id,cr.Comment as Comments,cr.Question2 as MeetingStatus,cr.Createdon,cr.Name as CreatedBy From CandidateRatings cr join Interviewmeetings im on im.id=cr.Interviewid where im.interviewer=@Emailid    
end
END
GO