/****** Object:  Table [dbo].[PoorQOSZoom]    Script Date: 04-12-2023 ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'PoorQOSZoom')
BEGIN
CREATE TABLE [dbo].[PoorQOSZoom](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ZoomMeetingId] [varchar] (200) NOT NULL,
	[participantEmail] [varchar](100) NULL,
	[qosType] [varchar](50) NULL,
	[max_jitter] [varchar](15) NULL,
	[min_jitter] [varchar](15) NULL,
	[avg_jitter] [varchar](15) NULL,
	[quality] [varchar](15) NULL,
	[quality_score] [varchar](15) NULL,
	[max_latency] [varchar](15) NULL,
	[avg_loss] [varchar](15) NULL,
	[max_loss] [varchar](15) NULL,
	[user_name] [varchar](50) NULL,
	[CreatedAt][datetime] NULL,
 CONSTRAINT [PK_PoorQOSZoom] PRIMARY KEY CLUSTERED 
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
    WHERE Name IN (N'[user_name]')
      AND Object_ID = Object_ID(N'PoorQOSZoom'))
BEGIN
    ALTER TABLE PoorQOSZoom
    Add [user_name] varchar(50)
END


End
Go