IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClonableSkillChannel')
BEGIN  
CREATE TABLE [dbo].[ClonableSkillChannel](					
			[id] [int] IDENTITY(1,1) NOT NULL,			
			[Name] [VARCHAR](200) NOT NULL,
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
			[ModifiedOn][DATETIME] NULL,
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
			[BillingOwner][int] NULL,
			[AssessmentGroupId][int] NULL,
            [IsQCRequired][bit] NULL,
            [IsJoinabilitySelectabilityRequired][bit] NULL,
            [IsGeneralSkillRequired][bit] NULL,
			CONSTRAINT [FK_ClonableSkillChannel_Client] FOREIGN KEY (ClientId) REFERENCES Client(id),
			CONSTRAINT [FK_ClonableSkillChannel_User] FOREIGN KEY (RisebirdHr) REFERENCES UserList(id),
 CONSTRAINT [PK_ClonableSkillChannel] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
