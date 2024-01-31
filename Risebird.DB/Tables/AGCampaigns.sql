/****** Object:  Table [dbo].[AGCampaigns]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'AGCampaigns')
BEGIN
CREATE TABLE [dbo].[AGCampaigns](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[AssessmentGroupId] [int] NOT NULL,
	[CampaignName] [varchar](50) NOT NULL,
	[CreatedAt][datetime] Null,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](100) NOT NULL,
	[ModifiedBy][varchar](100) NULL,
	CONSTRAINT FK_AGCampaigns_AssessmentGroupId FOREIGN KEY([AssessmentGroupId]) REFERENCES [dbo].[AssessmentGroup] ([id]),
 CONSTRAINT [PK_AGCampaigns] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] --TEXTIMAGE_ON [PRIMARY]
END

GO


