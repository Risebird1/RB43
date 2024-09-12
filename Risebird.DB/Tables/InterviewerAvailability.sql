/****** Object:  Table [dbo].[InterviewerAvailability]    Script Date: 11-12-2023 5.14.41 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerAvailability')
BEGIN 

Create TABLE [dbo].[InterviewerAvailability](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerProfileId][int] NOT NULL,
	[AvailabilityDate][datetime] Not NULL,
	[Hour_00_TO_01] [bit] NULL,
	[Hour_01_TO_02] [bit] NULL,
	[Hour_02_TO_03] [bit] NULL,
	[Hour_03_TO_04] [bit] NULL,
	[Hour_04_TO_05] [bit] NULL,
	[Hour_05_TO_06] [bit] NULL,
	[Hour_06_TO_07] [bit] NULL,
	[Hour_07_TO_08] [bit] NULL,
	[Hour_08_TO_09] [bit] NULL,
	[Hour_09_TO_10] [bit] NULL,
	[Hour_10_TO_11] [bit] NULL,
	[Hour_11_TO_12] [bit] NULL,
	[Hour_12_TO_13] [bit] NULL,
	[Hour_13_TO_14] [bit] NULL,
	[Hour_14_TO_15] [bit] NULL,
	[Hour_15_TO_16] [bit] NULL,
	[Hour_16_TO_17] [bit] NULL,
	[Hour_17_TO_18] [bit] NULL,
	[Hour_18_TO_19] [bit] NULL,
	[Hour_19_TO_20] [bit] NULL,
	[Hour_20_TO_21] [bit] NULL,
	[Hour_21_TO_22] [bit] NULL,
	[Hour_22_TO_23] [bit] NULL,
	[Hour_23_TO_00] [bit] NULL,
	[CreatedBy][varchar](100) NULL,
	[CreatedOn][datetime] Not NULL,
	[ModifiedBy][varchar](100) NULL,
	[ModifiedOn][datetime] NULL,
	CONSTRAINT [FK_InterviewerAvailability_InterviewerProfileId] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_InterviewerAvailability] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End

GO

