/****** Object:  Table [dbo].[UnvettedInterviewers]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'UnvettedInterviewers')
BEGIN
CREATE TABLE [dbo].[UnvettedInterviewers](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[EmailId] [varchar](100) NOT NULL,
	[MobileNumber] [varchar](35) NOT NULL,
	[PrimaryAssessmentGroupIds] [varchar](150) NOT NULL, -- from the list of assessment groups that we have
	[YearsOfExperience] [int] NOT NULL,
	[ProfileType] [int] NOT NULL,
	[ResumeLink] [varchar](1500) NOT NULL,
	[IsVetted] [bit] NULL, --NULL means not vetted yet by anyone, 0 means discarded by the Vetter, 1 means approved by the vetter(the changes suggested by the vetter can be checked in the InterviewerProfile table)
	[VettedBy] [varchar](100) NULL,
	[CreatedAt][datetime] Null,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](100) NOT NULL,
	[ModifiedBy][varchar](100) NULL,
 CONSTRAINT [PK_UnvettedInterviewers] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] --TEXTIMAGE_ON [PRIMARY]
END

GO

