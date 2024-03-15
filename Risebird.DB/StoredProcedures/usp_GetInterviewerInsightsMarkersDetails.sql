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
@type varchar(50),
@Irfid varchar(15)
AS
BEGIN
if(@IrfId='')
begin
set @IrfId=null
end
if(@type Like 'Redo%')
begin
Select ri.id,ri.Comments,ri.CreatedBy,ri.Createdon,ri.Inappropriate as MeetingStatus From RedoInterviews ri join InterviewMeetings im on im.id=ri.interviewid where im.interviewer=@Emailid and im.RedoInterview=1 and (@IrfId is null or im.IRFId=@IrfId)
end
else if(@type Like 'Suspicious%')
begin
Select si.id,si.Comment as Comments ,si.CreatedBy,si.Createddate as Createdon From SuspiciousInterviewer si join UserList ul on ul.id=si.UserId where SuspiciousStatus=1 and ul.EmailId=@Emailid    
end
else if(@type Like 'Client Rating%')
begin
Select cr.id,cr.Comment as Comments,cr.CreatedBy,cr.CreatedAt as Createdon,cr.Rating as MeetingStatus  From ClientCommentsForReport cr join Interviewmeetings im on im.id=cr.Interviewid where im.interviewer=@Emailid and (@IrfId is null or im.IRFId=@IrfId)   
end
else if(@type Like 'Candidate Rating%')
begin
Select  cfd.id,cfd.Rating as MeetingStatus,cfd.Createdon,(Convert(varchar(20),im.interviewDate)+' : '+im.candidateEmail) as Comments From CandidateFeedbackDetails cfd join Interviewmeetings im on im.id=cfd.Interviewid where im.interviewer=@Emailid and (@IrfId is null or im.IRFId=@IrfId)   
end
END
GO