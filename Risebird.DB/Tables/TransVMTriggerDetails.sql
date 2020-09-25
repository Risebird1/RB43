/****** Object:  Table [dbo].[TransVMTriggerDetails]    Script Date: 16-09-2020 6.39.00 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransVMTriggerDetails')
BEGIN 


CREATE TABLE [dbo].[TransVMTriggerDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[VMPropertyId] [int] NOT NULL,
	[LastAction] [varchar](100) NOT NULL,
	[ExpectedStatus] [varchar](100) NOT NULL,
	[sCreatedBy] [varchar](50) NOT NULL,
	[dtCreatedOn] [datetime] NOT NULL,
	[sModifiedBy] [varchar](50) NOT NULL,
	[dtModifiedOn] [datetime] NOT NULL,
 CONSTRAINT [PK_TransVMTriggerDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO

ALTER TABLE [dbo].[TransVMTriggerDetails] ADD  CONSTRAINT [DF_TransVMTriggerDetails_sCreatedBy]  DEFAULT ('NA') FOR [sCreatedBy]
GO

ALTER TABLE [dbo].[TransVMTriggerDetails] ADD  CONSTRAINT [DF_TransVMTriggerDetails_dtCreatedOn]  DEFAULT (getdate()) FOR [dtCreatedOn]
GO

ALTER TABLE [dbo].[TransVMTriggerDetails] ADD  CONSTRAINT [DF_TransVMTriggerDetails_sModifiedBy]  DEFAULT ('NA') FOR [sModifiedBy]
GO

ALTER TABLE [dbo].[TransVMTriggerDetails] ADD  CONSTRAINT [DF_TransVMTriggerDetails_dtModifiedOn]  DEFAULT (getdate()) FOR [dtModifiedOn]
GO

ALTER TABLE [dbo].[TransVMTriggerDetails]  WITH CHECK ADD  CONSTRAINT [FK_VMTransTriggerDetails_VMProperties] FOREIGN KEY([VMPropertyId])
REFERENCES [dbo].[VMProperties] ([id])
GO

ALTER TABLE [dbo].[TransVMTriggerDetails] CHECK CONSTRAINT [FK_VMTransTriggerDetails_VMProperties]
GO



