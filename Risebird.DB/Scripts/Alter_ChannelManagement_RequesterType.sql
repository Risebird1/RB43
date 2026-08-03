-- Idempotent ChannelManagement columns for public intake / verified vs external
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'RequesterType' AND Object_ID = Object_ID(N'ChannelManagement'))
    ALTER TABLE ChannelManagement ADD RequesterType varchar(15) NULL;
GO

-- 'Public' needs more than varchar(5)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'Origin' AND Object_ID = Object_ID(N'ChannelManagement')
      AND max_length > 0 AND max_length < 20)
BEGIN
    ALTER TABLE ChannelManagement ALTER COLUMN Origin varchar(20) NULL;
END
GO
