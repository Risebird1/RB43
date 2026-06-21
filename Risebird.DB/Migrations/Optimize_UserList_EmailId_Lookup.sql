-- Migration: Covering index for UserList lookups by EmailId (login, menu, activity)
-- Supports sp_GetMenuBasedOnEmailId and related EmailId filters

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_UserList_EmailId_Lookup'
      AND object_id = OBJECT_ID(N'dbo.UserList')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserList_EmailId_Lookup
    ON dbo.UserList (EmailId)
    INCLUDE (id, idProfile, LastLogin, IsActive);
END
GO
