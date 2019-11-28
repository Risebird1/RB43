/****** Object:  Table [dbo].[CandidatesMaster]    Script Date: 27-11-2019 7.25.06 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidatesMaster')
BEGIN 

CREATE TABLE [dbo].[CandidatesMaster](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Name] [varchar](200) NULL,
	[EmailId] [varchar](200) NULL,
	[ResumeLink] [varchar](500) NULL,
	[TotalInvites] [int] NULL,
	[Type] [varchar](100) NULL,
	[Location] [varchar](100) NULL,
	[Skill] [varchar](500) NULL,
	[WorkAuth] [varchar](100) NULL,
	[CurrentLocation] [varchar](100) NULL,
	[CurrentEmployer] [varchar](200) NULL,
	[Phone] [nvarchar](200) NULL,
	[TotalExperience] [decimal](10, 2) NULL,
	[CurrentSalary] [decimal](10, 2) NULL,
	[ExpectedSalary] [decimal](10, 2) NULL,
	[Notice] [decimal](10, 2) NULL,
	CONSTRAINT [FK__CandidatesMaster] FOREIGN KEY (IRFId) REFERENCES IRF(id),
 CONSTRAINT [PK_CandidatesMaster] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO



