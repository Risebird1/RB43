IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerAccDetails')
BEGIN
CREATE TABLE [dbo].[TransInterviewerAccDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TransInterviewerAccDetailsId] [int] NOT NULL,
	[AccountName] [varchar](200) NULL,
	[AccountNumber] [varchar](200) NULL,
	[IFSCCode] [varchar](200) NULL,
	[UPIId] [varchar](250) NULL,
	[PAN] [varchar](200) NULL,
	
	CONSTRAINT [FK__IProfileID_Acc] FOREIGN KEY (TransInterviewerAccDetailsId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerAccDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) 
)

END

ELSE
BEGIN

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AccountHolderName')
      AND Object_ID = Object_ID(N'TransInterviewerAccDetails'))
BEGIN
    ALTER TABLE TransInterviewerAccDetails
    DROP COLUMN  AccountHolderName
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Location')
      AND Object_ID = Object_ID(N'TransInterviewerAccDetails'))
BEGIN
    ALTER TABLE TransInterviewerAccDetails
    DROP COLUMN  Location
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PAN')
      AND Object_ID = Object_ID(N'TransInterviewerAccDetails'))
BEGIN
    ALTER TABLE TransInterviewerAccDetails
    Add PAN nvarchar(200) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AccountName')
      AND Object_ID = Object_ID(N'TransInterviewerAccDetails'))
BEGIN
    ALTER TABLE TransInterviewerAccDetails
    Add AccountName varchar (200) NULL
END

END