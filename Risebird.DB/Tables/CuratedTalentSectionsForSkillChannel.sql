IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CuratedTalentSectionsForSkillChannel')
BEGIN 

CREATE TABLE [dbo].[CuratedTalentSectionsForSkillChannel](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[CommunicationSkills] [bit] NULL,
	[VerbalAptitude] [bit] NULL,
	[QuantitativeAptitude] [bit] NULL,
	[PsychometricTest] [bit] NULL,
	[DataInterpretation] [bit] NULL,
	[DataSufficiency] [bit] NULL,
	[CriticalReasoning] [bit] NULL,
	[CreatedBy] [varchar](100) NULL,
	[CreatedAt] [datetime] NULL,
	[ModifiedBy] [datetime] NULL,
	[ModifiedAt] [datetime] NULL,

 CONSTRAINT [PK_CuratedTalentSectionsForSkillChannel] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END