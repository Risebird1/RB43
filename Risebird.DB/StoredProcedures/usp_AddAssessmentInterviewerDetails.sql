IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddAssessmentInterviewerDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddAssessmentInterviewerDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_AddAssessmentInterviewerDetails
@Assessmentid int,
@emailid varchar(200)
AS
BEGIN
Declare @UserId int
Select @UserId=id From UserList where Emailid=@emailid
IF NOT EXISTS(Select 1 from InterviewerAssessmentVerifications where AssessmentId=@Assessmentid and Userid=@UserId)
BEGIN
Insert into InterviewerAssessmentVerifications(AssessmentId,UserId,IsVerified) values(@Assessmentid,@UserId,0)
END
END
GO