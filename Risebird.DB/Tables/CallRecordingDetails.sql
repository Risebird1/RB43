/****** Object:  Table [dbo].[CallRecordingDetails]    Script Date: 23-07-2021 21:55:36 ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CallRecordingDetails')
BEGIN

CREATE TABLE [dbo].[CallRecordingDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[IRFId] [varchar](50) NULL,
	[RecordingUrl] [varchar](1000) NULL,
	[Sid] [varchar](50) NULL,
	[CandidateEmail] [varchar](150) NULL,
	[WhoCalled] [varchar](150) NULL,
	[CalledOn] [datetime] NULL,
 CONSTRAINT [PK_CallRecordingDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


