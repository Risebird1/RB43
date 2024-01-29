/****** Object:  Table [dbo].[CampaignSkillChannels]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CampaignSkillChannels')
BEGIN
CREATE TABLE [dbo].[CampaignSkillChannels](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[CampaignId] [int] NOT NULL,
	[IRFId] [int] NOT NULL,
	[CreatedAt][datetime] Null,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](100) NOT NULL,
	[ModifiedBy][varchar](100) NULL,
	CONSTRAINT FK_CampaignSkillChannels_CampaignId FOREIGN KEY([CampaignId]) REFERENCES [dbo].[AGCampaigns] ([id]),
 CONSTRAINT [PK_CampaignSkillChannels] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] --TEXTIMAGE_ON [PRIMARY]
END

GO


