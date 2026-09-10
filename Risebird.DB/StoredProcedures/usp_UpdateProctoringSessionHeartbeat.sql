IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateProctoringSessionHeartbeat'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateProctoringSessionHeartbeat
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateProctoringSessionHeartbeat]
    @SessionToken VARCHAR(64),
    @ClientState VARCHAR(16)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ProctoringSessions
    SET [ClientState] = @ClientState,
        [LastHeartbeatAtUtc] = SYSUTCDATETIME()
    WHERE [SessionToken] = @SessionToken
END
GO
