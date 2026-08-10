-- Migration: Index for UserSessions lookup in sp_GetMenuBasedOnEmailId

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_UserSessions_SessionId_IsActive'
      AND object_id = OBJECT_ID(N'dbo.UserSessions')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserSessions_SessionId_IsActive
    ON dbo.UserSessions (SessionId, IsActive)
    INCLUDE (UserId);
END
GO
