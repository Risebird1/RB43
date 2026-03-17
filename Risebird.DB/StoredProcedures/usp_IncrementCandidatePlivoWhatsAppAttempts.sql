-- =============================================
-- Author:      Plivo WhatsApp Integration
-- Create date: 2026-03-16
-- Description: Atomically increments PlivoWhatsAppAttempts on the Candidate row
--              identified by @CandidateId. Called immediately after each successful
--              Plivo CX WhatsApp trigger (schedule or reschedule) so the count stays
--              in sync with actual messages sent.
--
--              Kept separate from usp_IncrementCandidateVapiCallAttempts (voice)
--              so WhatsApp and voice attempt counts remain independently trackable.
-- =============================================

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_IncrementCandidatePlivoWhatsAppAttempts'))
BEGIN
    DROP PROCEDURE dbo.usp_IncrementCandidatePlivoWhatsAppAttempts
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_IncrementCandidatePlivoWhatsAppAttempts]
    @CandidateId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Candidate
    SET PlivoWhatsAppAttempts = ISNULL(PlivoWhatsAppAttempts, 0) + 1
    WHERE id = @CandidateId;
END
GO
