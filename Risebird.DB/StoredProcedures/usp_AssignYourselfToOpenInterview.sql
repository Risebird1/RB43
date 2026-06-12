IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AssignYourselfToOpenInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_AssignYourselfToOpenInterview
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AssignYourselfToOpenInterview]
    @meetingId int,
    @interviewerEmail varchar(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @irfIntId int
    DECLARE @irfName varchar(50)
    DECLARE @isLocked bit
    DECLARE @assignPolicy varchar(20)
    DECLARE @cap int
    DECLARE @assigned int
    DECLARE @isActive bit
    DECLARE @userId int
    DECLARE @currentInterviewer varchar(200)
    DECLARE @isVideoRequired bit
    DECLARE @channelDefaultCap int
    DECLARE @now datetime = DATEADD(MINUTE, 330, GETDATE())

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @currentInterviewer = im.interviewer,
               @irfName = im.IRFId,
               @irfIntId = irf.id,
               @isLocked = ISNULL(irf.DmIvLockNew, 0),
               @assignPolicy = ISNULL(NULLIF(LTRIM(RTRIM(irf.DmAssignmentPolicy)), ''), 'free'),
               @isVideoRequired = ISNULL(irf.IsVideoRequired, 0),
               @channelDefaultCap = ISNULL(NULLIF(irf.DmDefaultInterviewCap, 0), 20)
        FROM InterviewMeetings im WITH (UPDLOCK, ROWLOCK)
        JOIN IRF irf ON irf.IRFID = im.IRFId
        WHERE im.id = @meetingId;

        IF @irfIntId IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS Success, 'NOT_FOUND' AS ResultCode, 'Interview not found.' AS Message, 0 AS Assigned, 0 AS Capacity;
            RETURN;
        END

        IF @currentInterviewer IS NOT NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS Success, 'ALREADY_ASSIGNED' AS ResultCode, 'This interview has already been assigned.' AS Message, 0 AS Assigned, 0 AS Capacity;
            RETURN;
        END

        SELECT @userId = ul.id,
               @isActive = ti.IsActive,
               @cap = CASE
                   WHEN ti.InterviewCapacity IS NOT NULL THEN ti.InterviewCapacity
                   ELSE @channelDefaultCap
               END
        FROM TransInterviewer ti WITH (UPDLOCK, HOLDLOCK)
        JOIN UserList ul ON ul.id = ti.UserId
        WHERE ti.IRFId = @irfIntId AND ul.EmailId = @interviewerEmail;

        IF @userId IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS Success, 'NOT_ON_PANEL' AS ResultCode, 'You are not on the panel for this channel.' AS Message, 0 AS Assigned, 0 AS Capacity;
            RETURN;
        END

        IF @isLocked = 1 AND ISNULL(@isActive, 0) = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS Success, 'INACTIVE' AS ResultCode, 'You are not active on this channel and cannot be assigned.' AS Message, 0 AS Assigned, 0 AS Capacity;
            RETURN;
        END

        -- Cap enforcement follows assignment policy (not lock). Free policy allows all assignments.
        IF LOWER(@assignPolicy) = 'limited'
        BEGIN
            SELECT @assigned = COUNT(im.id)
            FROM InterviewMeetings im WITH (UPDLOCK, HOLDLOCK)
            WHERE im.IRFId = @irfName
              AND im.interviewer = @interviewerEmail
              AND im.interviewer IS NOT NULL
              AND im.interviewDate > @now
              AND ISNULL(im.IsDeleted, 0) = 0
              AND ISNULL(im.isAutoCancelled, 0) = 0
              AND CAST(im.confirmationStatus AS varchar(20)) NOT IN ('2', 'Cancelled')

            IF @assigned >= @cap
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT 0 AS Success, 'CAP_REACHED' AS ResultCode,
                       'Interview limit reached.' AS Message,
                       @assigned AS Assigned, @cap AS Capacity;
                RETURN;
            END
        END

        UPDATE InterviewMeetings
        SET interviewer = @interviewerEmail
        WHERE id = @meetingId AND interviewer IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS Success, 'ALREADY_ASSIGNED' AS ResultCode, 'This interview has already been assigned.' AS Message, 0 AS Assigned, 0 AS Capacity;
            RETURN;
        END

        COMMIT TRANSACTION;

        SELECT 1 AS Success, 'OK' AS ResultCode,
               CASE WHEN @isVideoRequired = 1
                    THEN 'Interview assigned Successfully|VIDEO_MANDATE'
                    ELSE 'Interview assigned Successfully.' END AS Message,
               0 AS Assigned, 0 AS Capacity;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT 0 AS Success, 'ERROR' AS ResultCode, ERROR_MESSAGE() AS Message, 0 AS Assigned, 0 AS Capacity;
    END CATCH
END
GO
