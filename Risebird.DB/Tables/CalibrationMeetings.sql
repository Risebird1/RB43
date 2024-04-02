IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CalibrationMeetings')
BEGIN 
CREATE TABLE [dbo].[CalibrationMeetings](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Irfid] [int] NULL,
	[MeetingDate] [datetime] NULL,
	[MeetingLink] [varchar](500) NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [varchar](150) NULL,
	[VideoLink] [varchar](1000) NULL,
	CONSTRAINT [FK_CalibrationIRF] FOREIGN KEY([Irfid]) REFERENCES [dbo].[IRF] ([id]),
 CONSTRAINT [PK_CalibrationMeetings] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO



