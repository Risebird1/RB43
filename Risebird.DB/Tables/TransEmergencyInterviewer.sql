/****** Object:  Table [dbo].[TransEmergencyInterviewer]    Script Date: 12-11-2019 6.01.38 PM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransEmergencyInterviewer')
BEGIN
CREATE TABLE [dbo].[TransEmergencyInterviewer](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[InterviewerId] [int] NOT NULL,
	[UserId] [int] NULL,
    [IsActive] [bit] NOT NULL default 1,
    [ClientAction] [Varchar](12) NULL,
	CONSTRAINT [FK__TransEmergencyInterviewer] FOREIGN KEY (IRFId) REFERENCES IRF(id),
	CONSTRAINT [FK__TransEmergencyInterviewer_1] FOREIGN KEY (InterviewerId) REFERENCES Interviewer(id),
 CONSTRAINT [PK_TransEmergencyInterviewer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerSuccessRate')
      AND Object_ID = Object_ID(N'TransEmergencyInterviewer'))
BEGIN
    ALTER TABLE TransEmergencyInterviewer
    Add PerSuccessRate INT
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsActive')
      AND Object_ID = Object_ID(N'TransEmergencyInterviewer'))
BEGIN
    ALTER TABLE TransEmergencyInterviewer
    Add IsActive bit NOT NULL default 1
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientAction')
      AND Object_ID = Object_ID(N'TransEmergencyInterviewer'))
BEGIN
    ALTER TABLE TransEmergencyInterviewer
    Add ClientAction varchar(12)
END

End
GO





