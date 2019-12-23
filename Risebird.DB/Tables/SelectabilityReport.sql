/****** Object:  Table [dbo].[SelectabilityReport]    Script Date: 10-12-2019 7.03.41 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SelectabilityReport')
BEGIN 


CREATE TABLE [dbo].[SelectabilityReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[RequiredLevelId] [int] NOT NULL,
	[Type] [nvarchar](200) NOT NULL,
	[ActualLevelId] [int] NOT NULL,
	[WeightageRequired] [int] NOT NULL,
	[WeightageActual] [int] NOT NULL,
	[Value] [varchar](500) NULL,
	[CandidateId] [int] NULL,
	[IRFId] [int] NULL,
	CONSTRAINT [FK_CandidateId_1] FOREIGN KEY (CandidateId) REFERENCES CandidatesMaster(id),
	FOREIGN KEY([ActualLevelId]) REFERENCES [dbo].[Level] ([id]),
	FOREIGN KEY([RequiredLevelId]) REFERENCES [dbo].[Level] ([id]),
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),

 CONSTRAINT [PK_SelectabilityReport] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO



