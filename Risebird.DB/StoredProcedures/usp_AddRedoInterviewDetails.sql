IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddRedoInterviewDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddRedoInterviewDetails
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_AddRedoInterviewDetails
@interviewid int,
@CandidateFace varchar(20),
@RequiredDuration int,
@ActualDuration int,
@Inappropriate varchar(20),
@HandsOfCoding varchar(20),
@Comments varchar(500),
@ModifiedBy varchar(200),
@ModifiedOn datetime
AS
BEGIN
IF EXISTS(select * from RedoInterviews where interviewid=@interviewid)
BEGIN
Update RedoInterviews set  interviewid=@interviewid,CandidateFace=@CandidateFace,Inappropriate=@Inappropriate,RequiredDuration=@RequiredDuration,
ActualDuration=@ActualDuration,HandsOfCoding=@HandsOfCoding,Comments=@Comments,RejectionComment=@RequiredDuration,ModifiedOn=@ModifiedOn,ModifiedBy=@ModifiedBy,
Type=null
where interviewid=@interviewid
END
ELSE
BEGIN
Insert into RedoInterviews(interviewid,CandidateFace,Inappropriate,RequiredDuration,ActualDuration,HandsOfCoding,Comments,
RejectionComment,Createdon,CreatedBy,ModifiedOn,ModifiedBy) values
(@interviewid,@CandidateFace,@Inappropriate,@RequiredDuration,@ActualDuration,@HandsOfCoding,@Comments,
null,@ModifiedOn,@ModifiedBy,@ModifiedOn,@ModifiedBy)
END 

-- QC Activity (RedoRequested): each save is logged in InterviewerActionLog (Reason starts
-- "Interview #<id>:", read by usp_QCActivity_CollectRows). Skipped while the log table is missing.
IF OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL AND NULLIF(LTRIM(RTRIM(@ModifiedBy)), '') IS NOT NULL
BEGIN
    Declare @RedoIvEmail varchar(200) = (Select TOP 1 LTRIM(RTRIM(interviewer)) From InterviewMeetings where id=@interviewid)
    INSERT INTO dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
    SELECT (Select TOP 1 ul.id From UserList ul where ul.EmailId=@RedoIvEmail ORDER BY ul.id DESC),
           @RedoIvEmail,
           'RedoRequested',
           LEFT(N'Interview #' + CAST(@interviewid AS NVARCHAR(20)) + N': ' + ISNULL(NULLIF(LTRIM(RTRIM(@Comments)), ''), N'redo requested'), 500),
           LEFT(LTRIM(RTRIM(@ModifiedBy)), 200),
           'usp_AddRedoInterviewDetails'
END
END
Go