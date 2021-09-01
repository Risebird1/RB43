IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InstaZoomMeetingDetails')
BEGIN

CREATE TABLE [dbo].[InstaZoomMeetingDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[MeetingStart] [datetime] NULL,
	[MeetingEnd] [datetime] NULL,
	[Accountid] [int] NULL,
 CONSTRAINT [PK_InstaZoomMeetingDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


