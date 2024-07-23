/****** Object:  UserDefinedTableType [dbo].[TransInterviewer_TableTypeV3]    Script Date: 23-07-2024 ******/
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransInterviewer_TableTypeV3')
BEGIN

CREATE TYPE [dbo].[TransInterviewer_TableTypeV3] AS TABLE(
	[IRFId] [int] NOT NULL,
	[UserId] [int] NULL,
	[PerSuccessRate] [int] NULL,
	[IsActive] [bit] not null,
	[IsAvailable] [bit] not null,
	[Source] [varchar](75) null,
	[IsCampaignActivity][bit] null,
	[CampaignId][int] null,
	[OpenAIScreeningResponse][varchar](max) null,
	[AIVerdict][varchar](20) null,
	[IsSurchargeApprove] [bit] NULL
)

End
GO


