 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_undoSuspiciousInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_undoSuspiciousInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- @InterviewerProfileId is the interviewer's UserList.id (SuspiciousInterviewer.UserId).
-- @ModifiedBy is the session email of the user undoing the mark (QC Activity: SuspiciousUndone).
-- When an active mark existed, the mark is stamped with ModifyBy / ModifyDate (UTC) and one
-- InterviewerActionLog row is written (skipped while that table is missing).
-- Returns one row: RowsChanged = active marks cleared (0 when the interviewer had no active
-- mark or the id is unknown), so the page can show an error instead of a false success.
Create Procedure usp_undoSuspiciousInterviewer
@InterviewerProfileId int,
@ModifiedBy varchar(200) = NULL
AS
BEGIN
SET NOCOUNT ON;

DECLARE @HadActiveMark bit = 0;
DECLARE @ActiveMarks int = (SELECT COUNT(1) FROM SuspiciousInterviewer WHERE UserId = @InterviewerProfileId AND SuspiciousStatus = 1);
DECLARE @MarkComment varchar(100) = NULL;
SELECT TOP 1 @HadActiveMark = 1, @MarkComment = Comment
FROM SuspiciousInterviewer
WHERE UserId = @InterviewerProfileId AND SuspiciousStatus = 1
ORDER BY Id DESC;

update SuspiciousInterviewer set
    ModifyBy   = CASE WHEN SuspiciousStatus = 1 THEN ISNULL(LEFT(@ModifiedBy, 100), ModifyBy) ELSE ModifyBy END,
    ModifyDate = CASE WHEN SuspiciousStatus = 1 THEN GETUTCDATE() ELSE ModifyDate END,
    SuspiciousStatus=0
where UserId=@InterviewerProfileId

IF @HadActiveMark = 1 AND OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
    SELECT @InterviewerProfileId,
           (SELECT TOP 1 ul.EmailId FROM UserList ul WHERE ul.id = @InterviewerProfileId),
           'SuspiciousUndone',
           CASE WHEN NULLIF(LTRIM(RTRIM(@MarkComment)), '') IS NOT NULL THEN N'Mark: ' + LTRIM(RTRIM(@MarkComment)) END,
           NULLIF(LTRIM(RTRIM(@ModifiedBy)), ''),
           'usp_undoSuspiciousInterviewer';
END

SELECT ISNULL(@ActiveMarks, 0) AS RowsChanged;
END
GO
