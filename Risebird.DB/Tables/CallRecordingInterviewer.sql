IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CallRecordingInterviewer')
BEGIN
CREATE TABLE [dbo].[CallRecordingInterviewer](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewer] [varchar](200) NULL,
	[RecordingUrl] [varchar](50) NULL,
	[Sid] [varchar](50) NULL,
	[WhoCalled] [varchar](150) NULL,
	[Calledon] [datetime] NULL,
 CONSTRAINT [PK_CallRecordingInterviewer] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


