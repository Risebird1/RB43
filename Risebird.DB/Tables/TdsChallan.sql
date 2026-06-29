IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TdsChallan')
BEGIN
CREATE TABLE [dbo].[TdsChallan](
    [id]                    INT            IDENTITY(1,1) NOT NULL,
    [FinancialYear]         NVARCHAR(9)    NOT NULL,
    [Quarter]               NVARCHAR(20)   NOT NULL,
    [WeekNumber]            INT            NOT NULL,
    [SrNo]                  INT            NOT NULL,
    [SectionCode]           NVARCHAR(10)   NOT NULL CONSTRAINT DF_TdsChallan_SectionCode   DEFAULT ('194JB'),
    [TANNumber]             NVARCHAR(15)   NULL,
    [Deductor]              NVARCHAR(200)  NULL,
    [TdsAmount]             DECIMAL(18,2)  NOT NULL DEFAULT 0,
    [BSRCode]               NVARCHAR(10)   NULL,
    [DepositDate]           DATE           NULL,
    [ChallanSerialNumber]   NVARCHAR(30)   NULL,
    [MinorHead]             NVARCHAR(10)   NOT NULL CONSTRAINT DF_TdsChallan_MinorHead      DEFAULT ('200'),
    [ChallanReferenceNumber] NVARCHAR(50)  NULL,
    [ChallanPdfPath]        NVARCHAR(400)  NULL,
    [PayoutReference]       NVARCHAR(100)  NULL,
    [PaymentDate]           DATE           NULL,
    [IsPaid]                BIT            NOT NULL CONSTRAINT DF_TdsChallan_IsPaid         DEFAULT (0),
    [CreatedOn]             DATETIME       NOT NULL CONSTRAINT DF_TdsChallan_CreatedOn      DEFAULT (GETDATE()),
    [CreatedBy]             NVARCHAR(150)  NULL,
    [UpdatedOn]             DATETIME       NULL,
    [UpdatedBy]             NVARCHAR(150)  NULL,
    CONSTRAINT [PK_TdsChallan] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
END
GO
