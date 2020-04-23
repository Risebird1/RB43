IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerEducation')
BEGIN 


CREATE TABLE [dbo].[TransInterviewerEducation](
	[Id] [int] IDENTITY(1,1) NOT NULL,  -- PK
	[InterviewerProfileId] [int] NOT NULL, -- FK
	[Graduation] [nvarchar] (100) NULL,
	[CollegeType] [nvarchar] (100) NULL,
	[Specialization] [nvarchar] (100) NULL,
	[Score] [nvarchar] (50) NULL,
	CONSTRAINT [FK__IProfileID_Education] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransEducation] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) 

END

GO