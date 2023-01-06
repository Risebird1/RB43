/****** Object:  Table [dbo].[AssessmentGroupInterviewers]    Script Date: 13-01-2021 5.14.41 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'AssessmentGroupInterviewers')
BEGIN 

Create TABLE [dbo].[AssessmentGroupInterviewers](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[AssessmentGroupId] [int] NOT NULL,
	[InterviewerProfileId][int] NOT NULL,
	[IsSME] [bit] NULL,
	[CreatedBy][varchar](100) NOT NULL,
	[CreatedOn][datetime] NULL,
	[ModifiedBy][varchar](100) NULL,
	[ModifiedAt][datetime] NULL
	
 CONSTRAINT [PK_AssessmentGroupInterviewers] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End

GO

