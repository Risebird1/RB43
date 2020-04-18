IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerProfile')
BEGIN 

CREATE TABLE [dbo].[InterviewerProfile](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[FirstName] [varchar](150) NULL,
	[LastName] [varchar](150) NULL,
	[NickName] [varchar](100) NOT NULL, 
	[EmailId] [varchar](150) NOT NULL,
	[AltEmailID] [varchar](150) NULL,
	[Location] [varchar](200) NULL,
	[MobileNumber] [nvarchar](200) NOT NULL,
	[AltMobileNumber] [nvarchar](200) NULL,
	[CurrentRole] [varchar](150) NULL,
	[Experience] [decimal](10,2) NULL,
	[PreferredDomain] [varchar](200) NULL,
	[ResumeLink] [nvarchar] (max) NOT NULL,
	[CodedResumeLink] [varchar](max) NOT NULL,
	[IsLeadInterviewer] [bit] NULL,
	[IsActive] [bit] NOT NULL DEFAULT 0,
	[IsApproved] [bit] NOT NULL DEFAULT 0,
	
	CONSTRAINT [UC_InterviewerProfile] UNIQUE (NickName),
	CONSTRAINT [FK_InterviewerProfile] FOREIGN KEY (UserId) REFERENCES UserList(id),	
	CONSTRAINT [PK_InterviewerProfile] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) 


END

GO

