IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientCommentsForReport')

Begin
	CREATE TABLE [dbo].[ClientCommentsForReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[Comment] [varchar](max) NULL,
	[CreatedAt][datetime] NULL,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](50) NULL,
	[Rating][int] NULL,
	CONSTRAINT [FK_ClientCommentsForReport_InterviewId] FOREIGN KEY([InterviewId]) REFERENCES [dbo].[InterviewMeetings] ([id]),
	CONSTRAINT [PK_ClientCommentsForReport] PRIMARY KEY CLUSTERED 
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
    WHERE Name IN (N'Rating')
      AND Object_ID = Object_ID(N'ClientCommentsForReport'))
BEGIN
    ALTER TABLE ClientCommentsForReport
    Add Rating INT 
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedBy')
      AND Object_ID = Object_ID(N'ClientCommentsForReport'))
BEGIN
    ALTER TABLE ClientCommentsForReport
    drop column ModifiedBy  
END


End

Go