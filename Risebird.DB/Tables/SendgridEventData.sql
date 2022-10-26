/****** Object:  Table [dbo].[CallRecordingData]   ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SendgridEventData')
BEGIN

CREATE TABLE [dbo].[SendgridEventData](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[email] [varchar](50) NULL,
	[timestamp] [varchar](50) NULL,
	[event] [varchar](25) NULL,
	[smtp-id] [varchar](50) NULL,
	[ip] [varchar](50) NULL,
	[category] [varchar](50) NULL,
	[sg_event_id] [varchar](200) NULL,
	[sg_message_id] [varchar](200) NULL,
	[response][varchar](40) NULL,
	[url][varchar](500) NULL,
	[useragent][varchar](200) NULL,
	[reason][varchar](100) NULL,
	[status][varchar](50) NULL,
	[CreatedAt] datetime Null,
    [Split_Message_Id][varchar](50) NULL
 CONSTRAINT [PK_SendgridEventData] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
Else
Begin

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SendgridEventData')
      AND Object_ID = Object_ID(N'Split_Message_Id'))
BEGIN
    ALTER TABLE SendgridEventData
    Add [Split_Message_Id] varchar(50)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SendgridEventData')
      AND Object_ID = Object_ID(N'sg_message_id'))
BEGIN
    ALTER TABLE SendgridEventData
    Alter column [sg_message_id] varchar(200)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SendgridEventData')
      AND Object_ID = Object_ID(N'sg_message_id'))
BEGIN
    ALTER TABLE SendgridEventData
    Alter column [sg_event_id] varchar(200)
END

End
GO