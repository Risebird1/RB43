/****** Object:  Table [dbo].[RisebirdCandidateCommunicationCount]   ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'RisebirdCandidateCommunicationCount')
BEGIN

CREATE TABLE [dbo].[RisebirdCandidateCommunicationCount](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NULL,
	[MailCount] [int] NULL,
	[WhatsappCount] [int] NULL,
	[CallsCount] [int] NULL,
	[CreatedOn] datetime Null,
	[ModifiedOn] datetime Null
 CONSTRAINT [PK_RisebirdCandidateCommunicationCount] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
Else
Begin
	IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IRFId')
      AND Object_ID = Object_ID(N'RisebirdCandidateCommunicationCount'))
BEGIN
    ALTER TABLE RisebirdCandidateCommunicationCount
    Add IRFId INT 
END
End

GO

