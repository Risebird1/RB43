/****** Object:  Table [dbo].[UnapprovedInterviewerMeetings]    Script Date: 12-02-2021 11.50.53 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UnapprovedInterviewerMeetings](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ISCId] [varchar](200) NULL,
	[interviewDate] [datetime] NULL,
	[interviewSlot] [varchar](100) NOT NULL,
	[interviewDuration] [int] NULL,
	[candidateEmail] [varchar](200) NOT NULL,
	[confirmationStatus] [varchar](100) NOT NULL,
	[zoomLink] [varchar](1000) NOT NULL,
	[ZoomId] [varchar](200) NULL,
	[interviewer] [varchar](200) NULL,
	[HRCoordinatorEmailId] [varchar](100) NULL,
	[HRComment] [varchar](1000) NULL,
	[HRImageFileURL] [varchar](1000) NULL,
	[sCreatedBy] [varchar](100) NOT NULL DEFAULT 'System',
	[dtCreatedOn] [datetime] NOT NULL DEFAULT getutcdate(),
	[sModifiedBy] [varchar](100) NOT NULL DEFAULT 'System',
	[dtModifiedOn] [varchar](100) NOT NULL DEFAULT getutcdate(),
	[candidateName] [varchar](200) NULL,
 CONSTRAINT [PK_UnapprovedInterviewerMeetings] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO



