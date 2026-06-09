-- Migration: Add inactivation tracking columns to UserList
-- Distinguishes system-generated vs manual inactivation and records who did it

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'InactivationType' AND Object_ID = Object_ID(N'UserList'))
BEGIN
    ALTER TABLE UserList ADD [InactivationType] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'InactivatedBy' AND Object_ID = Object_ID(N'UserList'))
BEGIN
    ALTER TABLE UserList ADD [InactivatedBy] VARCHAR(200) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'InactivatedAt' AND Object_ID = Object_ID(N'UserList'))
BEGIN
    ALTER TABLE UserList ADD [InactivatedAt] DATETIME NULL
END
GO
