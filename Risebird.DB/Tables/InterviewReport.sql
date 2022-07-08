/****** Object:  Table [dbo].[InterviewReport]    Script Date: 7/6/2022 1:25:42 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewReport')

BEGIN
CREATE TABLE [dbo].[InterviewReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[InterviewId] [int] NOT NULL,
	[CandidateName] [nvarchar](100) NULL,
	[confirmationStatus] [nvarchar](100) NULL,
	[OverallScore] [float] NULL,
	[Rating] [int] NULL,
	[TechSkillComments] [varchar](max) NULL,
	[SkillGeneralComments] [varchar](max) NULL,
	[JoinabilityComments] [varchar](max) NULL,
	[SelectabilityComments] [varchar](max) NULL,
	[ProctoringCount] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[ModifiedOn] [datetime] NULL,
	[CreatedBy] [varchar](100) NULL,
	[ModifiedBy] [varchar](100) NULL,
	FOREIGN KEY (InterviewId) REFERENCES InterviewMeetings(id),
	FOREIGN KEY (IRFId) REFERENCES IRF(id),
	FOREIGN KEY (Rating) REFERENCES [Level](id),
 CONSTRAINT [PK_InterviewReport] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
End

Else
Begin
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CreatedOn')
      AND Object_ID = Object_ID(N'InterviewReport'))
BEGIN
    ALTER TABLE InterviewReport
    Add CreatedOn datetime
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedOn')
      AND Object_ID = Object_ID(N'InterviewReport'))
BEGIN
    ALTER TABLE InterviewReport
    Add ModifiedOn datetime
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CreatedBy')
      AND Object_ID = Object_ID(N'InterviewReport'))
BEGIN
    ALTER TABLE InterviewReport
    Add CreatedBy varchar(100)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedBy')
      AND Object_ID = Object_ID(N'InterviewReport'))
BEGIN
    ALTER TABLE InterviewReport
    Add ModifiedBy varchar(100)
END
End
Go



