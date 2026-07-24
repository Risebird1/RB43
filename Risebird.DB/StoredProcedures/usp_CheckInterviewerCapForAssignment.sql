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

    DECLARE @assignPolicy varchar(20)

    DECLARE @cap int

    DECLARE @assigned int

    DECLARE @irfName varchar(50)

    DECLARE @channelDefaultCap int

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

           @assignPolicy = ISNULL(NULLIF(LTRIM(RTRIM(DmAssignmentPolicy)), ''), 'free'),

           @irfName = IRFID,

           @channelDefaultCap = ISNULL(NULLIF(DmDefaultInterviewCap, 0), 20)

    FROM IRF

    WHERE id = @irfIntId



    DECLARE @isActive bit



    SELECT @isActive = ti.IsActive,

           @cap = CASE

               WHEN ti.InterviewCapacity IS NOT NULL THEN ti.InterviewCapacity

               ELSE @channelDefaultCap

           END

    FROM TransInterviewer ti

    JOIN UserList ul ON ul.id = ti.UserId

    WHERE ti.IRFId = @irfIntId AND ul.EmailId = @interviewerEmail



    IF @@ROWCOUNT = 0

    BEGIN

        SELECT 1 AS Allowed, @isLocked AS IsLocked, 0 AS IsInactive, 0 AS IsCapEnforced, 0 AS CapConfigured, 0 AS Assigned, 0 AS Capacity

        RETURN

    END



    IF @isLocked = 1 AND ISNULL(@isActive, 0) = 0

    BEGIN

        SELECT 0 AS Allowed, 1 AS IsLocked, 1 AS IsInactive, 0 AS IsCapEnforced, 0 AS CapConfigured, 0 AS Assigned, 0 AS Capacity

        RETURN

    END



    -- Free assignment policy: no cap enforcement (stored caps are ignored — Option B).

    IF LOWER(@assignPolicy) <> 'limited'

    BEGIN

        SELECT 1 AS Allowed, @isLocked AS IsLocked, 0 AS IsInactive, 0 AS IsCapEnforced, 0 AS CapConfigured, 0 AS Assigned, 0 AS Capacity

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

        SELECT 0 AS Allowed, @isLocked AS IsLocked, 0 AS IsInactive, 1 AS IsCapEnforced, 1 AS CapConfigured, @assigned AS Assigned, @cap AS Capacity

        RETURN

    END



    SELECT 1 AS Allowed, @isLocked AS IsLocked, 0 AS IsInactive, 1 AS IsCapEnforced, 1 AS CapConfigured, @assigned AS Assigned, @cap AS Capacity

END

GO

