IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIrfIntIdsByUserId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIrfIntIdsByUserId
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Ids of the active IRFs a UserList row takes part in, by the same five relationships that
-- usp_GetIrfIndexBatch uses to build irfindex.risebirdHrEmail and irfindex.allowedViewerEmails.
-- Both of those are denormalized snapshots of UserList.EmailId, so when a user is added, edited
-- or deleted these IRFs have to be re-indexed or the list view serves a stale name — or, for
-- allowedViewerEmails, stale visibility.
CREATE PROCEDURE [dbo].[usp_GetIrfIntIdsByUserId]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @UserId IS NULL OR @UserId <= 0
        RETURN;

    SELECT irf.id AS IrfIntId
    FROM dbo.IRF irf
    WHERE ISNULL(irf.IsDeleted, 0) = 0
      AND (
            -- Channel owner: drives both risebirdHr and risebirdHrEmail.
            irf.RisebirdHr = @UserId

            -- Client user.
            OR EXISTS (
                SELECT 1 FROM dbo.TransClientUser tc
                WHERE tc.IRFId = irf.id AND tc.ClientUserId = @UserId)

            -- Recruiter.
            OR EXISTS (
                SELECT 1 FROM dbo.TransRecruiter tr
                WHERE tr.IRFId = irf.id AND tr.UserId = @UserId)

            -- Senior HR is attached to the client, not the IRF, so this covers every IRF
            -- belonging to that client.
            OR EXISTS (
                SELECT 1 FROM dbo.SeniorHr sh
                WHERE sh.ClientId = irf.ClientId AND sh.UserId = @UserId)

            -- HR, via the HR row that points back at the user.
            OR EXISTS (
                SELECT 1
                FROM       dbo.TransHR th
                INNER JOIN dbo.HR hr ON hr.id = th.HRId
                WHERE th.IRFId = irf.id AND hr.UserId = @UserId)
          );
END
GO
