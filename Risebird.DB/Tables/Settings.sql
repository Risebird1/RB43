/****** Object:  Table [dbo].[Settings]    Script Date: 04-03-2020 11.37.49 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Settings')
BEGIN
CREATE TABLE [dbo].[Settings](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerUnassignedMinutes] [int] NULL,
	[AutoCancellationMailMinutes] [int] NULL,
	[ClientReportingMailMinutes] [int] NULL,
	[InterviewWindupReminderMinutes] [int] null,
	[InterviewReportReminderMinutes] [int] null,
	[QCCommentReminderMinutes] [int] null,
	[QCFormdeductionRate] [int] null,
 CONSTRAINT [PK_Settings] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'QCFormdeductionRate')
      AND Object_ID = Object_ID(N'Settings'))
BEGIN
    ALTER TABLE Settings
    Add QCFormdeductionRate int
END
END
GO


