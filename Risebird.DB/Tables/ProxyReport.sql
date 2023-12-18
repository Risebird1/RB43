IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ProxyReport')
BEGIN 
CREATE TABLE [dbo].[ProxyReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[interviewid] [int] NULL,
	[isProxyCandidate] [bit] NULL,
	[EyeMovement] [varchar](500) NULL,
	[LipsMovement] [varchar](500) NULL,
	[CandidateDesktop] [varchar](500) NULL,
	[LinkedinPhoto] [varchar](200) NULL,
	[HeadMovement] [varchar](500) NULL,
	[LatencyCheck] [varchar](500) NULL,
	[VoiceImpostering] [varchar](500) NULL,
	[SusipicousDevices] [varchar](500) NULL,
	[SuspiciousVoice] [varchar](500) NULL,
	[SuspiciousEnvironment] [varchar](500) NULL,
	[CreatedBy] [varchar](200) NULL,
	[CreatedOn] [datetime] NULL,
	[IsQCdone] [bit] NULL,
	[Qcdoneby] [varchar](200) NULL,
	[ProxyvotingCount] [int] NULL,
 CONSTRAINT [PK_ProxyReport] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


