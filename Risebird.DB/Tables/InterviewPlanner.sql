/****** Object:  Table [dbo].[InterviewPlanner]    Script Date: 11-10-2019 6.32.18 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewPlanner')
BEGIN

CREATE TABLE [dbo].[InterviewPlanner](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[OverAllProcessingTime] [int] NOT NULL DEFAULT 0,
	[InviteProcessingTime] [int] NOT NULL DEFAULT 0,
	[ConfirmationProcessingTime] [int] NOT NULL DEFAULT 0,
	[CompletionProcessingTime] [int] NOT NULL DEFAULT 0,
	[RisebirdSelectionProcessingTime] [int] NOT NULL DEFAULT 0,
	[ApprovalProcessingTime] [int] NOT NULL DEFAULT 0,
	[FinalSelectionProcessingTime] [int] NOT NULL DEFAULT 0,
	[OfferProcessingTime] [int] NOT NULL DEFAULT 0,
	[JoiningProcessingTime] [int] NOT NULL DEFAULT 0,
	[InviteOwnedBy] [varchar](200) NOT NULL DEFAULT 'NA',
	[ConfirmationOwnedBy] [varchar](200) NOT NULL DEFAULT 'NA',
	[CompletionOwnedBy] [varchar](200) NOT NULL DEFAULT 'NA',
	[RisebirdSelectionOwnedBy] [varchar](200) NOT NULL DEFAULT 'NA',
	[ApprovalOwnedBy] [varchar](200) NOT NULL DEFAULT 'NA',
	[FinalSelectionOwnedBy] [varchar](200) NOT NULL DEFAULT 'NA',
	[OfferOwnedBy] [varchar](200) NOT NULL DEFAULT 'NA',
	[JoiningOwnedBy] [varchar](200) NOT NULL DEFAULT 'NA',
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
 CONSTRAINT [PK_InterviewPlanner] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
GO





