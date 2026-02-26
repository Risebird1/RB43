-- =============================================
-- Author: Vapi Integration
-- Create date: 2026-02-25
-- Description: Checks whether a confirmed interview already exists in InterviewMeetings
--              for a given candidate email and IRF (identified by IRF.id INT).
--
-- Key relationship:
--   @IrfId    → IRF.id (INT, primary key)
--   IRF.IRFId → VARCHAR business identifier stored in InterviewMeetings.IRFId
--
-- Returns: 1 if a confirmed interview exists (confirmationStatus = '1'), 0 otherwise.
-- =============================================

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_HasConfirmedInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_HasConfirmedInterview
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_HasConfirmedInterview]
    @IrfId         INT,
    @CandidateEmail VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- Resolve IRF.id → IRF.IRFId (VARCHAR), then check InterviewMeetings
    IF EXISTS (
        SELECT 1
        FROM InterviewMeetings im
        INNER JOIN IRF irf ON irf.IRFId = im.IRFId
        WHERE irf.id              = @IrfId
          AND im.candidateEmail   = @CandidateEmail
          AND im.confirmationStatus = '1'
    )
        SELECT CAST(1 AS BIT) AS HasConfirmed;
    ELSE
        SELECT CAST(0 AS BIT) AS HasConfirmed;
END
GO
