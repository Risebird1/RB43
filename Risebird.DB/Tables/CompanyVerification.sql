-- Advanced Resume Screening: cache of company-registry lookups used by the authenticity checks
-- (one row per normalised employer name), plus the internal blocklist of fake-experience vendors.
-- Rows older than 90 days are looked up again. A blocklisted row is never overwritten by a lookup.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'CompanyVerification')
BEGIN
CREATE TABLE [dbo].[CompanyVerification](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[NormalizedName] [nvarchar](300) NOT NULL,
	[DisplayName] [nvarchar](300) NULL,
	[Found] [bit] NULL,
	[CIN] [varchar](30) NULL,
	[Status] [varchar](50) NULL,
	[IncorporationDate] [date] NULL,
	[StruckOffDate] [date] NULL,
	[EmployeeBand] [varchar](30) NULL,
	[Website] [varchar](300) NULL,
	[DomainAgeMonths] [int] NULL,
	[Source] [varchar](100) NULL,
	[CheckedOn] [datetime] NULL,
	[IsBlocklisted] [bit] NOT NULL CONSTRAINT [DF_CompanyVerification_IsBlocklisted] DEFAULT (0),
	[BlocklistReason] [nvarchar](1000) NULL,
	[BlocklistedBy] [varchar](200) NULL,
	[BlocklistedOn] [datetime] NULL,
 CONSTRAINT [PK_CompanyVerification] PRIMARY KEY CLUSTERED ([id] ASC),
 CONSTRAINT [UQ_CompanyVerification_NormalizedName] UNIQUE ([NormalizedName])
) ON [PRIMARY]
END
GO
