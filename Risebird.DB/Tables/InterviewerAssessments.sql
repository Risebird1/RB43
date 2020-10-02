IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerAssessments')
BEGIN 

CREATE TABLE [dbo].[InterviewerAssessments](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Assessmentgroupid] [int] NULL,
	[Overview] [varchar](max) NULL,
	[Senioritylevel] [int] NULL,
	[YearsofExprience] [int] NULL,
	[AssessmentType] [int] NULL,
	[TestedSkill] [varchar](200) NULL,
	[Evaluation] [int] NULL,
	[Environment] [varchar](100) NULL,
	[Difficultylevel] [int] NULL,
	[EmployerType] [varchar](50) NULL,
	[EmployerStage] [int] NULL,
	[EmployerSize] [int] NULL,
	[isApproved] [bit] NULL,
	[createdby] [int] NULL,
	[createdon] [datetime] NULL,
	[Delivery] [varchar](200) NULL,
 CONSTRAINT [PK_InterviewerAssessments] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


