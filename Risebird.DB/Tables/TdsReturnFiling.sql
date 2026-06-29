IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TdsReturnFiling')
BEGIN
CREATE TABLE [dbo].[TdsReturnFiling](
    [id]                    INT           IDENTITY(1,1) NOT NULL,
    [FinancialYear]         NVARCHAR(9)   NOT NULL,
    [Quarter]               NVARCHAR(20)  NOT NULL,
    [ReturnType]            NVARCHAR(10)  NOT NULL CONSTRAINT DF_TdsReturnFiling_ReturnType DEFAULT ('26Q'),
    [FilingDate]            DATE          NULL,
    [AcknowledgementNumber] NVARCHAR(50)  NULL,
    [TokenNumber]           NVARCHAR(50)  NULL,
    [Status]                NVARCHAR(20)  NULL,
    [Remarks]               NVARCHAR(500) NULL,
    [CreatedOn]             DATETIME      NOT NULL CONSTRAINT DF_TdsReturnFiling_CreatedOn  DEFAULT (GETDATE()),
    [CreatedBy]             NVARCHAR(150) NULL,
    [UpdatedOn]             DATETIME      NULL,
    [UpdatedBy]             NVARCHAR(150) NULL,
    CONSTRAINT [PK_TdsReturnFiling] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_TdsReturnFiling_FyQuarter] UNIQUE ([FinancialYear], [Quarter])
) ON [PRIMARY]
END
GO
