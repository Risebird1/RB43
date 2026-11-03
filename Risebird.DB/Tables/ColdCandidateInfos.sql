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

-- WhatsApp unsubscribe -> cold (usp_MarkCandidateColdForWhatsAppOptOut): where the cold mark came from, e.g.
-- "WhatsApp unsubscribe button (Schedule Interview)" or "WhatsApp Stop reply", and the reason the candidate picked.
-- NULL for a cold mark set by a person in the portal. The role is the candidate row itself (FKCandidateId ->
-- Candidate.IRFId -> IRF): one cold row per candidate per role.
-- Call opt-out uses the same columns: ColdSource = AICall (AI calling agent) | HumanCall (Mark cold popup "Opted out
-- on call"), shown as the "Opted out: AI call / Human call" badge; ColdReason = the opt-out quote or popup reason.
IF COL_LENGTH('dbo.ColdCandidateInfos', 'ColdSource') IS NULL
    ALTER TABLE dbo.ColdCandidateInfos ADD ColdSource varchar(150) NULL;
GO
IF COL_LENGTH('dbo.ColdCandidateInfos', 'ColdReason') IS NULL
    ALTER TABLE dbo.ColdCandidateInfos ADD ColdReason nvarchar(300) NULL;
GO
