IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetBounceMailContent'))
BEGIN
    DROP PROCEDURE dbo.usp_GetBounceMailContent
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Stored mail (subject + HTML body) behind a Bounce & Dropped Mail row. Used by the "Content" link
-- (loaded on click instead of embedding every body in the page) and by Resend Mail.
-- Access is checked by the caller (usp_GetBounceDroppedRowForAction) before this runs.
CREATE PROCEDURE [dbo].[usp_GetBounceMailContent]
@SendgridEventId int
AS
BEGIN
SET NOCOUNT ON;

SELECT TOP 1 smd.id AS SentMailDetailsId, smd.MailSubject, smd.MailBody, smd.IRFId, smd.InterviewId
FROM SendgridEventData sge
JOIN SentMailDetails smd ON smd.sg_message_id = sge.Split_Message_Id
WHERE sge.id = @SendgridEventId
ORDER BY smd.id DESC;
END
GO
