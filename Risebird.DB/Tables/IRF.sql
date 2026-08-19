IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'IRF')
BEGIN  
CREATE TABLE [dbo].[IRF](					
			[id] [int] IDENTITY(1,1) NOT NULL,			
			[IRFId] [VARCHAR](200) NOT NULL,
			[ClientId] [int] NOT NULL,			
			[SkillCluster] [VARCHAR](200) NULL,
			[Location] [VARCHAR](200) NULL,
			[StartDate][DateTime] NOT NULL,
			[EndDate][DateTime] NOT NULL,
			[JDLink][VARCHAR](MAX) NULL,
			[NoOfPosition] [int] NULL,	
			[NoOfIntterview][int] NULL,
			[RisebirdHr] [int] NOT NULL,
			[TotalInvites][int] NOT NULL DEFAULT 0,
			[CalendlyLink][VARCHAR](500) NULL,
			[TotalBudget] [int] NULL,
			[CreatedOn][DATETIME] NULL,
			[Discount] [real] NOT NULL,
			[IsBiometricEnabled] [bit] NOT NULL,
			[PriorityLevel] [int] NOT NULL,
			[IsPaused] [bit] NOT NULL,
            [CompanyDetails][VARCHAR](MAX) NULL,
            [Comment][VARCHAR](250) NULL,
            [BussinessType][varchar](4) NULL,
            [Heuristicsscore][int] NULL,
            [idProgress][int] NOT NULL DEFAULT 1,
            [PrimarySkillInvite][varchar](100) NULL,
            [IsQCRequired][bit] NULL,
            [IsJoinabilitySelectabilityRequired][bit] NULL,
            [IsGeneralSkillRequired][bit] NULL,
            [AudioComment][VARCHAR](1000) NULL,
            [CreatedFromTemplateId][int] NULL,
            [ModifiedOn][Datetime] NULL,
            [SendIntervieweravailbilityOn][Datetime] NULL,
            [MeetingPlatform][varchar](25) NULL,
            [IsAadhaarVerificationRequired][bit] NULL,
            [IsCandidateavailability][bit] NULL,
            [IsMeetingNotificationSend][bit] NULL,
            [IsMeetingAutoCancelled][bit] NULL,
            [OriginalJDLink][varchar](2000) NULL,
            [AudioCommentBy][varchar](100) NULL,
            [UpdatedBySME][int] NULL,
            [UpdatedBySMEAt][datetime] NULL,
            [IsLocked][bit] null,
            [flagcount][int] null,
            [InterviewDuration][int] null,
            [IsFixedInterviewduration][bit] null,
            [FixedAmount][int] null,
            [IsNewSkillChannel] [bit] null,
            [FlatRate] [int] null,
            [RateComputedForClient] [int] null,
            [IsMarketPlaceChannel] [bit] null,
            [IsAffindaIndexCreated] [bit] null,
            [AffindaEmbedUrl] [varchar](2500) null,
            [IsDrive] [bit] null,
            [AffindaEmbedUrlCreatedAt] [datetime] null,
            [DMApproved] [bit] null,
            [DMApprovedBy] [varchar](150)Null,
            [SkillSetsevaluate] [int]Null,
            [JDTitleTwo] [VARCHAR](150) null,
            [IsVideoRequired] [bit] null,
            [TechRubricType] [VARCHAR](20) null,
            [RequestId] [NVARCHAR](50) null,
            [CandidateIdverification] [bit] null,
            [CodeSnippetRequired] [bit] null,
            [CandidateVideo] [VARCHAR](1000) null,
            [IsAIInterview] [bit] null,
            [AIInterviewType] [int] null,
            [OpenAIAssistantId]  [VARCHAR](60) null,
            [OpenAIPromptId]  [VARCHAR](100) null,
            [InterviewerBillingCycle]  [int] null,
            [IsTechQC]  [bit] null,
            [showChatBox]  [bit] null,
            [generateTranscriptSummary]  [bit] null,
            [isMultiLingual]  [bit] null,
            [IsProctoring]  [bit] null,
			CONSTRAINT [FK_IRF_Client] FOREIGN KEY (ClientId) REFERENCES Client(id),
			CONSTRAINT [FK_IRF_User] FOREIGN KEY (RisebirdHr) REFERENCES UserList(id),
 CONSTRAINT [PK_IRF] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

ELSE
BEGIN


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TotalInvites')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add TotalInvites INT NOT NULL DEFAULT 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CalendlyLink')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add CalendlyLink VARCHAR(500)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TotalBudget')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add TotalBudget int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CreatedOn')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add CreatedOn datetime
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsBiometricEnabled')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsBiometricEnabled BIT NOT NULL DEFAULT 0
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Discount')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add Discount real NOT NULL DEFAULT 0
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PriorityLevel')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add PriorityLevel INT NOT NULL DEFAULT 3
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsPaused')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsPaused BIT NOT NULL DEFAULT 0
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyDetails')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add CompanyDetails [VARCHAR](MAX) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Comment')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add Comment VARCHAR(250)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'idProgress')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
	ADD idProgress int not null default 1
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BussinessType')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
	ADD BussinessType varchar(4) NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Heuristicsscore')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
	ADD Heuristicsscore int NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PrimarySkillInvite')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add PrimarySkillInvite VARCHAR(100)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BillingOwner')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add BillingOwner int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AssessmentGroupId')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add AssessmentGroupId varchar(100)
	END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsQCRequired')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsQCRequired bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsJoinabilitySelectabilityRequired')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsJoinabilitySelectabilityRequired bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsGeneralSkillRequired')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsGeneralSkillRequired bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AudioComment')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add AudioComment varchar(1000)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CreatedFromTemplateId')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add CreatedFromTemplateId int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedOn')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    Alter table IRF 
    add ModifiedOn datetime NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SendIntervieweravailbilityOn')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    Alter table IRF 
    add SendIntervieweravailbilityOn datetime NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'MeetingPlatform')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add MeetingPlatform VARCHAR(25)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsAadhaarVerificationRequired')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsAadhaarVerificationRequired bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsCandidateavailability')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsCandidateavailability bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsMeetingNotificationSend')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsMeetingNotificationSend bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsMeetingAutoCancelled')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsMeetingAutoCancelled bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'OriginalJDLink')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add OriginalJDLink VARCHAR(2000)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AudioCommentBy')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add AudioCommentBy VARCHAR(100)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'UpdatedBySME')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add UpdatedBySME int null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'UpdatedBySMEAt')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add UpdatedBySMEAt datetime null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsLocked')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsLocked bit null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'flagcount')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add flagcount int null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'InterviewDuration')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add InterviewDuration int null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsFixedInterviewduration')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsFixedInterviewduration bit null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'FixedAmount')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add FixedAmount INT NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsNewSkillChannel')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsNewSkillChannel bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'FlatRate')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add FlatRate INT NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'RateComputedForClient')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add RateComputedForClient int
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsMarketPlaceChannel')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsMarketPlaceChannel bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsAffindaIndexCreated')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsAffindaIndexCreated bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AffindaEmbedUrl')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add AffindaEmbedUrl varchar(2500)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsDrive')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsDrive bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DMApproved')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add DMApproved bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DMApprovedBy')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add DMApprovedBy varchar(150)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AffindaEmbedUrlCreatedAt')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add AffindaEmbedUrlCreatedAt datetime
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SkillSetsevaluate')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add SkillSetsevaluate int
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'JDTitleTwo')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add JDTitleTwo VARCHAR(150)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsVideoRequired')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsVideoRequired bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TechRubricType')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add TechRubricType VARCHAR(20)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'RequestId')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add RequestId NVARCHAR(50)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateIdverification')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add CandidateIdverification bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CodeSnippetRequired')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add CodeSnippetRequired bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateVideo')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add CandidateVideo varchar(1000)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsAIInterview')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
	ADD IsAIInterview bit 
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AIInterviewType')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
	ADD AIInterviewType int 
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'OpenAIAssistantId')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add OpenAIAssistantId varchar(60)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'InterviewerBillingCycle')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add InterviewerBillingCycle int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsTechQC')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsTechQC bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'showChatBox')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add showChatBox bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'generateTranscriptSummary')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add generateTranscriptSummary bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'isMultiLingual')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add isMultiLingual bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SkillCluster')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add SkillCluster VARCHAR(200) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsProctoring')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsProctoring bit null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DmIvLockNew')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add DmIvLockNew bit NOT NULL CONSTRAINT DF_IRF_DmIvLockNew DEFAULT 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DmAssignmentPolicy')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add DmAssignmentPolicy varchar(20) NOT NULL CONSTRAINT DF_IRF_DmAssignmentPolicy DEFAULT 'free'
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DmDefaultInterviewCap')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add DmDefaultInterviewCap int NOT NULL CONSTRAINT DF_IRF_DmDefaultInterviewCap DEFAULT 20
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'VoiceInstructionTranscript')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    ADD VoiceInstructionTranscript NVARCHAR(MAX) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'VoiceInstructionTranscriptUpdatedOn')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    ADD VoiceInstructionTranscriptUpdatedOn DATETIME NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DmEtiquetteDressCode')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    ADD DmEtiquetteDressCode varchar(20) NOT NULL CONSTRAINT DF_IRF_DmEtiquetteDressCode DEFAULT 'formal'
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DmEtiquetteNoEatingRule')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    ADD DmEtiquetteNoEatingRule bit NOT NULL CONSTRAINT DF_IRF_DmEtiquetteNoEatingRule DEFAULT 1
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'VoiceInstructionTranscriptUpdatedBy')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    ADD VoiceInstructionTranscriptUpdatedBy VARCHAR(100) NULL
END

END


GO
