IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'ClientAgreement')
BEGIN
    CREATE TABLE [dbo].[ClientAgreement](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [ClientId] [int] NOT NULL,
        [AgreementType] [int] NOT NULL,          -- 1=MSA, 2=SOW, 3=NDA, 4=DPA
        [StartDate] [datetime] NULL,
        [ExpireDate] [datetime] NULL,
        [Status] [int] NOT NULL CONSTRAINT [DF_ClientAgreement_Status] DEFAULT (1),  -- 1=Active, 2=Terminated ("Expired" is derived from ExpireDate)
        [AgreementFileUrl] [varchar](500) NULL,
        [Remarks] [varchar](500) NULL,
        [CreatedBy] [varchar](200) NULL,
        [CreatedOn] [datetime] NULL,
        [ModifiedBy] [varchar](200) NULL,
        [ModifiedOn] [datetime] NULL,
        CONSTRAINT [PK_ClientAgreement] PRIMARY KEY CLUSTERED ([id] ASC)
    ) ON [PRIMARY]

    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Client]') AND type IN (N'U'))
    BEGIN
        ALTER TABLE [dbo].[ClientAgreement] WITH CHECK ADD CONSTRAINT [FK_ClientAgreement_Client] FOREIGN KEY([ClientId])
        REFERENCES [dbo].[Client] ([id])
        ALTER TABLE [dbo].[ClientAgreement] CHECK CONSTRAINT [FK_ClientAgreement_Client]
    END
END
GO
