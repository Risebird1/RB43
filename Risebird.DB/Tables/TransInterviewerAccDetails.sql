IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerAccDetails')
BEGIN 


CREATE TABLE [dbo].[TransInterviewerAccDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[AccountHolderName] [varchar](200) NULL,
	[AccountNumber] [nvarchar](200) NULL,
	[IFSCCode] [nvarchar](200) NULL,
	[Location] [varchar](max) NULL,
	[UPIId] [nvarchar](250) NULL,
	CONSTRAINT [FK__IProfileID_Acc] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerAccDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)


END

GO