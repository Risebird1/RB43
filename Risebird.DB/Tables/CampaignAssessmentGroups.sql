/****** Object:  Table [dbo].[CampaignAssessmentGroups]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CampaignAssessmentGroups')
BEGIN
CREATE TABLE [dbo].[CampaignAssessmentGroups](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[CampaignId] [int] NOT NULL,
	[AssessmentGroupId] [int] NOT NULL,
	[CreatedAt][datetime] Null,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](100) NOT NULL,
	[ModifiedBy][varchar](100) NULL,
	CONSTRAINT FK_CampaignAssessmentGroups_CampaignId FOREIGN KEY([CampaignId]) REFERENCES [dbo].[AGCampaigns] ([id]),
 CONSTRAINT [PK_CampaignAssessmentGroups] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] --TEXTIMAGE_ON [PRIMARY]
END

GO


