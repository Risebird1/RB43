IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ResolveMailBounce'))
BEGIN
    DROP PROCEDURE dbo.usp_ResolveMailBounce
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Records a Mark Resolved / Unreachable / Resent / RetriedSameAddress action on a Bounce & Dropped Mail row.
-- Access is checked by the caller (usp_GetBounceDroppedRowForAction) before this runs.
-- Returns 1 when a row was inserted, 0 for an invalid @Status.
CREATE PROCEDURE [dbo].[usp_ResolveMailBounce]
@SendgridEventId int,
@CandidateId int = null,
@Status varchar(20),
@Note nvarchar(500) = null,
@ActionBy varchar(150)
AS
BEGIN
SET NOCOUNT ON;

-- RetriedSameAddress (QA round 1, Q1): a temporary bounce was resent to the same address. Closed like Resent.
IF (@Status NOT IN ('Resolved', 'Unreachable', 'Resent', 'RetriedSameAddress'))
BEGIN
    SELECT CAST(0 AS bit) AS Success;
    RETURN;
END

INSERT INTO MailBounceResolution (SendgridEventId, CandidateId, [Status], Note, ActionBy, ActionAt)
VALUES (@SendgridEventId, @CandidateId, @Status, NULLIF(LTRIM(RTRIM(@Note)), ''), @ActionBy, GETDATE());

SELECT CAST(1 AS bit) AS Success;
END
GO
