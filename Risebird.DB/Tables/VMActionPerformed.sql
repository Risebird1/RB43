/****** Object:  Table [dbo].[InterviewMeetings]    Script Date: 06-10-2020 4.13.34 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'VMActionPerformed')
BEGIN
CREATE TABLE [dbo].[VMActionPerformed](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[VmId] [int] NOT NULL,
	[Action] [varchar](30) NOT NULL,
	[PerformedBy] [varchar](100) NOT NULL,
	[PerformedOn] [datetime] NOT NULL,
	FOREIGN KEY([VmId]) REFERENCES [dbo].[VMProperties] ([id]),

CONSTRAINT [PK_VMActionPerformed] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


