IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CheckInterviewerCapForAssignment'))
BEGIN
    DROP PROCEDURE dbo.usp_CheckInterviewerCapForAssignment
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_CheckInterviewerCapForAssignment]
    @irfIntId int = NULL,
    @interviewerEmail varchar(200),
    @meetingId int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @isLocked bit
    DECLARE @cap int
    DECLARE @assigned int
    DECLARE @irfName varchar(50)
    DECLARE @defaultCap int

    IF @irfIntId IS NULL AND @meetingId IS NOT NULL
    BEGIN
        SELECT @irfIntId = irf.id
        FROM IRF irf
        JOIN InterviewMeetings im ON irf.IRFID = im.IRFId
        WHERE im.id = @meetingId
    END

    IF @irfIntId IS NULL
    BEGIN
        SELECT 1 AS Allowed, 0 AS IsLocked, 0 AS Assigned, 0 AS Capacity
        RETURN
    END

    SELECT @isLocked = ISNULL(DmIvLockNew, 0),
           @irfName = IRFID,
           @defaultCap = ISNULL(DmDefaultInterviewCap, 20)
    FROM IRF WHERE id = @irfIntId

    IF @isLocked = 0
    BEGIN
        SELECT 1 AS Allowed, 0 AS IsLocked, 0 AS Assigned, 0 AS Capacity
        RETURN
    END

    SELECT @cap = ISNULL(ti.InterviewCapacity, 0)
    FROM TransInterviewer ti
    JOIN UserList ul ON ul.id = ti.UserId
    WHERE ti.IRFId = @irfIntId AND ul.EmailId = @interviewerEmail

    IF @cap IS NULL OR @cap = 0
        SET @cap = @defaultCap

    SELECT @assigned = COUNT(im.id)
    FROM InterviewMeetings im
    WHERE im.IRFId = @irfName
      AND im.interviewer = @interviewerEmail
      AND im.interviewDate > DATEADD(MINUTE, 330, GETDATE())

    IF @assigned >= @cap
    BEGIN
        SELECT 0 AS Allowed, 1 AS IsLocked, @assigned AS Assigned, @cap AS Capacity
        RETURN
    END

    SELECT 1 AS Allowed, 1 AS IsLocked, @assigned AS Assigned, @cap AS Capacity
END
GO
