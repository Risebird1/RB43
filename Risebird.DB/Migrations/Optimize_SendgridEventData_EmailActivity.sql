-- Migration: Indexes to support usp_GetEmailRecipientsLatestActivity and related SendGrid lookups
-- Supports sargable CreatedAt/event filtering and latest-event-per-message partitioning

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SendgridEventData_CreatedAt_Event'
      AND object_id = OBJECT_ID(N'dbo.SendgridEventData')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SendgridEventData_CreatedAt_Event
    ON dbo.SendgridEventData (CreatedAt, [event])
    INCLUDE (email, Split_Message_Id, [timestamp]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_UserList_EmailId_idProfile'
      AND object_id = OBJECT_ID(N'dbo.UserList')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserList_EmailId_idProfile
    ON dbo.UserList (EmailId, idProfile);
END
GO
