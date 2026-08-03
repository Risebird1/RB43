-- Idempotent ChannelManagement columns for Requirement Management (MAG/Template mapping)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'MAGId' AND Object_ID = Object_ID(N'ChannelManagement'))
    ALTER TABLE ChannelManagement ADD MAGId int NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TemplateId' AND Object_ID = Object_ID(N'ChannelManagement'))
    ALTER TABLE ChannelManagement ADD TemplateId int NULL;
GO
