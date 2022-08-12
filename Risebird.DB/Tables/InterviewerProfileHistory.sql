IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerProfileHistory')

BEGIN
CREATE TABLE [dbo].[InterviewerProfileHistory](
	[id] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[NickName] [varchar](100) NOT NULL,
	[AltEmailID] [varchar](150) NULL,
	[DOB] [datetime] NULL,
	[Gender] [varchar](50) NULL,
	[AnnualSalary] [nvarchar](200) NULL,
	[Country] [varchar](200) NULL,
	[City] [varchar](200) NULL,
	[AltMobileNumber] [nvarchar](200) NULL,
	[CurrentRole] [varchar](150) NULL,
	[Experience] [decimal](10, 2) NULL,
	[PreferredDomain] [varchar](200) NULL,
	[GitHubLink] [varchar](200) NULL,
	[ResumeLink] [varchar](2000) NULL,
	[CodedResumeLink] [varchar](2000) NULL,
	[StackOverflowLink] [varchar](2000) NULL,
	[LinkedinLink] [varchar](2000) NULL,
	[Blog] [varchar](2000) NULL,
	[IsLeadInterviewer] [bit] NULL,
	[IsApproved] [bit] NOT NULL,
	[Tag] [varchar](200) NULL,
	[AssessmentGroupId] [varchar](100) NULL,
	[Isinstaavailable] [bit] NULL,
	[Isavailable] [int] NULL,
	[IsNotificationAccepted] [bit] NULL,
	[CreatedOn] [datetime] NULL,
	[ModifiedOn] [datetime] NULL,
	[CreatedBy] [varchar](100) NULL,
	[ModifiedBy] [varchar](100) NULL,
	[PerformedAt][datetime] NULL,
	[PerformedBy][varchar](100) NULL,
	[ActionType][varchar](15) NULL)
	

End

GO




