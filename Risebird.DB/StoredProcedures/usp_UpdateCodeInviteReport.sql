IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(' dbo.usp_UpdateCodeInviteReport'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCodeInviteReport
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateCodeInviteReport
@inviteid varchar(100),
@submissionId varchar(60),
@assignmentId varchar(60),
@submittedBy varchar(200),
@submittedTime datetime,
@score int,
@percentageScore int,
@totalAssignmentScore int,
@status varchar(20)
AS
BEGIN
update CodeInviteDetails set
submissionId=@submissionId,assignmentId=@assignmentId,submittedBy=@submittedBy,submittedTime=@submittedTime,score=@score,
percentageScore=@percentageScore,totalAssignmentScore=@totalAssignmentScore,status=@status
where inviteId=@inviteid
END
GO