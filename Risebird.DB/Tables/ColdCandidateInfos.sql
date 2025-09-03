IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ColdCandidateInfos')
BEGIN  
CREATE TABLE [dbo].[ColdCandidateInfos](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IsColdCandidate] [bit] NULL,
	[ColdMarkedBY] [varchar](100) NULL,
	[ColdMarkedDate] [datetime] NULL,
	[FKCandidateId] [int] NULL,
    [ModifyBy] [varchar](150) NULL,
	[ModifyOn] [datetime] NULL,
	[ColdInfo] [varchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[ColdCandidateInfos]  WITH CHECK ADD FOREIGN KEY([FKCandidateId])
REFERENCES [dbo].[Candidate] ([id])
End

ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifyBy')
      AND Object_ID = Object_ID(N'ColdCandidateInfos'))
BEGIN
    ALTER TABLE ColdCandidateInfos
    Add ModifyBy [varchar](150) NULL		
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifyOn')
      AND Object_ID = Object_ID(N'ColdCandidateInfos'))
BEGIN
    ALTER TABLE ColdCandidateInfos
    Add [ModifyOn] [datetime] NULL		
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ColdInfo')
      AND Object_ID = Object_ID(N'ColdCandidateInfos'))
BEGIN
    ALTER TABLE ColdCandidateInfos
    Add ColdInfo varchar(100) null
END
END
GO
