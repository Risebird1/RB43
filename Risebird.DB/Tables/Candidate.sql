/****** Object:  Table [dbo].[Candidate]    Script Date: 25-09-2019 7.49.24 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Candidate')
BEGIN
CREATE TABLE [dbo].[Candidate](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[Name] [varchar](200) NULL,
	[EmailId] [varchar](200) NULL,
	[ResumeLink] [varchar](500) NULL,
	[TotalInvites] INT NOT NULL DEFAULT 0,
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
 CONSTRAINT [PK_Candidate] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TotalInvites')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add TotalInvites INT DEFAULT 0
END
End

GO



