IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_OpsToolkit_RemoveUnassignPenalty'))
BEGIN
    DROP PROCEDURE dbo.usp_OpsToolkit_RemoveUnassignPenalty
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Ops Toolkit: removes an unassign penalty written by usp_applyInterviewerpenalty.
-- Sets the InterviewPoints row to 0 points (only while it is still 'Pending', i.e. not yet in a payment run),
-- drops the matching "Unassignment Interview Penalty" reliability score (type 11) and logs the action.
CREATE PROCEDURE [dbo].[usp_OpsToolkit_RemoveUnassignPenalty]
    @InterviewPointsId INT,
    @Reason            NVARCHAR(500),
    @PerformedBy       VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @InterviewId INT, @UserId INT, @Points INT, @Status VARCHAR(20), @Type VARCHAR(50);

    BEGIN TRY
        BEGIN TRAN;

        SELECT @InterviewId = ip.Interviewid,
               @UserId      = ip.userid,
               @Points      = ip.Points,
               @Status      = ip.Status,
               @Type        = ip.Type
        FROM InterviewPoints ip WITH (UPDLOCK, ROWLOCK)
        WHERE ip.id = @InterviewPointsId;

        IF @InterviewId IS NULL
        BEGIN
            ROLLBACK TRAN;
            SELECT CAST(0 AS BIT) AS Success, 'Penalty record not found.' AS Message;
            RETURN;
        END

        IF @Type <> 'Interview' OR ISNULL(@Points, 0) >= 0
        BEGIN
            ROLLBACK TRAN;
            SELECT CAST(0 AS BIT) AS Success, 'No unassign penalty on this record (already removed?).' AS Message;
            RETURN;
        END

        IF ISNULL(@Status, '') <> 'Pending'
        BEGIN
            ROLLBACK TRAN;
            SELECT CAST(0 AS BIT) AS Success, 'Penalty is already ' + ISNULL(@Status, 'processed') + ' and cannot be removed here.' AS Message;
            RETURN;
        END

        UPDATE InterviewPoints SET Points = 0 WHERE id = @InterviewPointsId;

        DELETE trs
        FROM TransInterviewersReliabilityScores trs
        JOIN InterviewerProfile ipr ON ipr.Id = trs.InterviewerId
        WHERE trs.InterviewMeetingId = @InterviewId
          AND trs.ReliabilityScoreTypeId = 11
          AND ipr.UserId = @UserId;

        INSERT INTO OpsToolkitActionLog (ActionType, InterviewId, InterviewPointsId, UserId, OldPoints, NewPoints, Reason, PerformedBy)
        VALUES ('RemoveUnassignPenalty', @InterviewId, @InterviewPointsId, @UserId, @Points, 0, @Reason, @PerformedBy);

        COMMIT TRAN;
        SELECT CAST(1 AS BIT) AS Success, 'Penalty removed.' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO
