IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerProfessional')
BEGIN 


CREATE TABLE [dbo].[TransInterviewerProfessional](
	[Id] [int] IDENTITY(1,1) NOT NULL,  -- PK
	[InterviewerProfileId] [int] NOT NULL, -- FK
	[CompanyName] [varchar] (200) NULL,
	[CompanyType] [nvarchar](50) NULL,
	[Domain] [nvarchar] (200) NULL,
	[StartDate] [datetime] NULL,
	[EndDate] [datetime] NULL,
	CONSTRAINT [FK__IProfileID_Professional] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransProfessional] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) 

END

GO