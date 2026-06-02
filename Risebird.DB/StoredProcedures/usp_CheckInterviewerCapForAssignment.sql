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

    DECLARE @capConfigured bit

    DECLARE @assigned int

    DECLARE @irfName varchar(50)

    DECLARE @now datetime = DATEADD(MINUTE, 330, GETDATE())



    IF @irfIntId IS NULL AND @meetingId IS NOT NULL

    BEGIN

        SELECT @irfIntId = irf.id

        FROM IRF irf

        JOIN InterviewMeetings im ON irf.IRFID = im.IRFId

        WHERE im.id = @meetingId

    END



    IF @irfIntId IS NULL

    BEGIN

        SELECT 1 AS Allowed, 0 AS IsLocked, 0 AS IsInactive, 0 AS IsCapEnforced, 0 AS CapConfigured, 0 AS Assigned, 0 AS Capacity

        RETURN

    END



    SELECT @isLocked = ISNULL(DmIvLockNew, 0),

           @irfName = IRFID

    FROM IRF WHERE id = @irfIntId



    IF @isLocked = 0

    BEGIN

        SELECT 1 AS Allowed, 0 AS IsLocked, 0 AS IsInactive, 0 AS IsCapEnforced, 0 AS CapConfigured, 0 AS Assigned, 0 AS Capacity

        RETURN

    END



    DECLARE @isActive bit



    SELECT @isActive = ti.IsActive,

           @cap = ti.InterviewCapacity,

           @capConfigured = CASE WHEN ti.InterviewCapacity IS NOT NULL THEN 1 ELSE 0 END

    FROM TransInterviewer ti

    JOIN UserList ul ON ul.id = ti.UserId

    WHERE ti.IRFId = @irfIntId AND ul.EmailId = @interviewerEmail



    IF ISNULL(@isActive, 0) = 0

    BEGIN

        SELECT 0 AS Allowed, 1 AS IsLocked, 1 AS IsInactive, 0 AS IsCapEnforced, 0 AS CapConfigured, 0 AS Assigned, 0 AS Capacity

        RETURN

    END



    IF @capConfigured = 0

    BEGIN

        SELECT 1 AS Allowed, 1 AS IsLocked, 0 AS IsInactive, 0 AS IsCapEnforced, 0 AS CapConfigured, 0 AS Assigned, 0 AS Capacity

        RETURN

    END



    SELECT @assigned = COUNT(im.id)

    FROM InterviewMeetings im

    WHERE im.IRFId = @irfName

      AND im.interviewer = @interviewerEmail

      AND im.interviewer IS NOT NULL

      AND im.interviewDate > @now

      AND ISNULL(im.IsDeleted, 0) = 0

      AND ISNULL(im.isAutoCancelled, 0) = 0

      AND CAST(im.confirmationStatus AS varchar(20)) NOT IN ('2', 'Cancelled')



    IF @assigned >= @cap

    BEGIN

        SELECT 0 AS Allowed, 1 AS IsLocked, 0 AS IsInactive, 1 AS IsCapEnforced, 1 AS CapConfigured, @assigned AS Assigned, @cap AS Capacity

        RETURN

    END



    SELECT 1 AS Allowed, 1 AS IsLocked, 0 AS IsInactive, 1 AS IsCapEnforced, 1 AS CapConfigured, @assigned AS Assigned, @cap AS Capacity

END

GO

