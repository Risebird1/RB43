-- Migration: Bounce & Dropped Mail page (bugs #7 and #8)
-- Run in a low-traffic window, BEFORE deploying the reworked usp_GetEmailRecipientsActivityBounceDropped.
--
-- #7  SendgridEventData.email was varchar(50): longer addresses were truncated and never matched UserList.
--     Increasing a varchar length is allowed even when the column is an index key/include column.
-- #8  The list SP now filters with CreatedAt >= @start AND CreatedAt < @endExclusive; this index serves it.

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SendgridEventData')
      AND name = N'email'
      AND max_length < 150
)
BEGIN
    ALTER TABLE dbo.SendgridEventData ALTER COLUMN email varchar(150) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SendgridEventData_Event_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.SendgridEventData')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SendgridEventData_Event_CreatedAt
    ON dbo.SendgridEventData ([event], CreatedAt)
    INCLUDE (email, Split_Message_Id, [timestamp], reason, [status], [url]);
END
GO

-- The bounce list joins SentMailDetails on sg_message_id; make sure that lookup is a seek.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal = 1
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.SentMailDetails') AND c.name = N'sg_message_id'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SentMailDetails_sg_message_id
    ON dbo.SentMailDetails (sg_message_id)
    INCLUDE (IRFId, InterviewId, CreatedAt, SentBy, MailSubject);
END
GO
