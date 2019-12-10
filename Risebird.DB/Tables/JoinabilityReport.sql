/****** Object:  Table [dbo].[JoinabilityReport]    Script Date: 10-12-2019 7.03.12 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'JoinabilityReport')
BEGIN 


CREATE TABLE [dbo].[JoinabilityReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[RequiredLevelId] [int] NOT NULL,
	[Type] [nvarchar](200) NOT NULL,
	[ActualLevelId] [int] NOT NULL,
	[WeightageRequired] [int] NOT NULL,
	[WeightageActual] [int] NOT NULL,
	[Value] [varchar](500) NULL,
	[CandidateId] [int] NULL,
	CONSTRAINT [FK_CandidateId] FOREIGN KEY (CandidateId) REFERENCES CandidatesMaster(id),
	FOREIGN KEY([ActualLevelId]) REFERENCES [dbo].[Level] ([id]),
	FOREIGN KEY([RequiredLevelId]) REFERENCES [dbo].[Level] ([id]),

 CONSTRAINT [PK_JoinabilityReport] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO



