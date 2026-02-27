-- =============================================
-- Author: Vapi Integration
-- Create date: 2026-02-25
-- Description: Atomically increments VapiCallAttempts on the Candidate row
--              identified by @CandidateId. Called immediately after each
--              successful Vapi call initiation so that the count stays
--              in sync with actual calls made.
-- =============================================

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_IncrementCandidateVapiCallAttempts'))
BEGIN
    DROP PROCEDURE dbo.usp_IncrementCandidateVapiCallAttempts
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_IncrementCandidateVapiCallAttempts]
    @CandidateId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Candidate
    SET VapiCallAttempts = VapiCallAttempts + 1
    WHERE id = @CandidateId;
END
GO
