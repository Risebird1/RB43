IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientAudioFeedbackForReport')

Begin
	CREATE TABLE [dbo].[ClientAudioFeedbackForReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[AudioUrl] [varchar](max) NULL,
	[CreatedAt][datetime] NULL,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](50) NULL,
	[IRFId][int] NULL,
	CONSTRAINT [PK_ClientAudioFeedbackForReport] PRIMARY KEY CLUSTERED 
	(
		[id] ASC
	) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
End

Else
Begin
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IRFId')
      AND Object_ID = Object_ID(N'ClientAudioFeedbackForReport'))
BEGIN
    ALTER TABLE ClientAudioFeedbackForReport
    Add [IRFId] int CONSTRAINT
	[FK_ClientAudioFeedbackForReport_IRFId] FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id])
END
End

Go

