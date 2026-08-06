-- Idempotent ChannelOwner column for Risebird Support Person
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ChannelOwner')
      AND Object_ID = Object_ID(N'ChannelManagement'))
BEGIN
    ALTER TABLE ChannelManagement ADD [ChannelOwner] varchar(150) NULL
END
GO
