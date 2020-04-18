IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerProjects')
BEGIN 


CREATE TABLE [dbo].[TransInterviewerProjects](
	[Id] [int] IDENTITY(1,1) NOT NULL,  -- PK
	[InterviewerProfileId] [int] NOT NULL, -- FK
	[ProjectName] [varchar] (200) NULL,
	[ClientName] [nvarchar](200) NULL,
	[StartDate] [datetime] NULL,
	[EndDate] [datetime] NULL,
	[Role] [nvarchar] (200) NULL,
	[ProjectDescriptions] [nvarchar] (max) NULL,
	CONSTRAINT [FK__IProfileID_Projects] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerProjects] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) 

END

GO

