IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ZoomMeetingTransmission')
BEGIN

CREATE TABLE [dbo].[ZoomMeetingTransmission](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Event] [varchar](100) NULL,
	[ParticipantName] [varchar](200) NULL,
	[Meetingid] [varchar](200) NULL,
	[Emailid] [varchar](200) NULL,
	[ActionDate] [Datetime] NULL,
 CONSTRAINT [PK_ZoomMeetingTransmission] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

	IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Meetingid')
      AND Object_ID = Object_ID(N'ZoomMeetingTransmission'))
	BEGIN
		alter table ZoomMeetingTransmission
		alter column Meetingid varchar(200)
	END
GO


