IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SkillChannelCalendlyDetails')

BEGIN

CREATE TABLE [dbo].[SkillChannelCalendlyDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[ResourceUri] [varchar](1000) NULL,
	[CreatedOn] [datetime] NOT NULL,
	--[CreatedBy] [int] NOT NULL,
	[ModifiedOn] [datetime] NULL,
	--[ModifiedBy] [int] NULL,
    CONSTRAINT [FK_SkillChannelCalendlyDetails_IRFId] FOREIGN KEY (IRFId) REFERENCES IRF(id),
    CONSTRAINT [UQ_SkillChannelCalendlyDetails_IRFId] unique (IRFId),

 CONSTRAINT [PK_SkillChannelCalendlyDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 

END

GO


