IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransChannelInterviewerList')

BEGIN 

CREATE TABLE [dbo].[TransChannelInterviewerList](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NULL,
	[Channelid] [int] NULL,
	[InterviewerProfileId] [int] NULL,
	[status] [varchar](10) NULL,
 CONSTRAINT [PK_TransChannelInterviewerList] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


