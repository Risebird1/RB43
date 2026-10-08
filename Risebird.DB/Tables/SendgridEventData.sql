/****** Object:  Table [dbo].[CallRecordingData]   ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SendgridEventData')
BEGIN

CREATE TABLE [dbo].[SendgridEventData](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[email] [varchar](150) NULL,
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
    WHERE Name IN (N'Split_Message_Id')
      AND Object_ID = Object_ID(N'SendgridEventData'))
BEGIN
    ALTER TABLE SendgridEventData
    Add [Split_Message_Id] varchar(50)
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'sg_message_id')
      AND Object_ID = Object_ID(N'SendgridEventData') AND max_length < 200)
BEGIN
    ALTER TABLE SendgridEventData
    Alter column [sg_message_id] varchar(200) NULL
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'sg_event_id')
      AND Object_ID = Object_ID(N'SendgridEventData') AND max_length < 200)
BEGIN
    ALTER TABLE SendgridEventData
    Alter column [sg_event_id] varchar(200) NULL
END

End
GO

-- Bounce & Dropped Mail page (bug #7): addresses longer than 50 chars were truncated.
-- Widening a varchar is a metadata-only change, but it needs an exclusive lock on this busy table. Waiting for
-- that lock would queue every SendGrid webhook insert behind it, so give up after 5 seconds instead; rerun later.
IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Object_ID = Object_ID(N'SendgridEventData')
      AND Name = N'email' AND max_length < 150)
BEGIN
    SET LOCK_TIMEOUT 5000;
    BEGIN TRY
        ALTER TABLE SendgridEventData
        Alter column [email] varchar(150) NULL
        PRINT 'SendgridEventData.email widened to varchar(150).';
    END TRY
    BEGIN CATCH
        PRINT 'SendgridEventData.email NOT widened: the table is busy (' + ERROR_MESSAGE() + '). Rerun this script in a quieter moment.';
    END CATCH
    SET LOCK_TIMEOUT -1;
END
GO
