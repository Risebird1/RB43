IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateProctoringSessionState'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateProctoringSessionState
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Single place that writes ProctoringSessions.[State] (idle | starting | running | stopped).
-- Called by usp_MarkProctoringSessionJoined/Started/Ended instead of each setting it inline,
-- so the lifecycle -> State mapping lives in exactly one stored procedure.
CREATE PROCEDURE [dbo].[usp_UpdateProctoringSessionState]
    @SessionToken VARCHAR(64),
    @State VARCHAR(16)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ProctoringSessions
    SET [State] = @State
    WHERE [SessionToken] = @SessionToken
END
GO
