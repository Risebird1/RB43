IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransZoomMeetingExtendDetails')
BEGIN

CREATE TABLE [dbo].[TransZoomMeetingExtendDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Meetingid] [varchar](25) NULL,
	[Joinedcount] [int] NULL,
	[Leftcount] [int] NULL,
	[WhoJoined] [varchar](max) NULL,
 CONSTRAINT [PK_TransZoomMeetingExtendDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


