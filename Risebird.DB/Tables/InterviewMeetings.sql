
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewMeetings')
BEGIN  
CREATE TABLE [dbo].[InterviewMeetings](		
	[id] [int] IDENTITY(1,1) NOT NULL,	
	[clientName] [varchar](200) NOT NULL,	
	[JobRole] [varchar](200)  NULL,	
	[interviewDate] [varchar](200) NOT NULL,	
	[interviewSlot] [varchar](100) NOT NULL,
	[interviewDuration] [varchar](50) NOT NULL,	
	[primaryskill] [varchar](500) NOT NULL,
	[candidateEmail] [varchar](200) NOT NULL,	
	[ClientHrContact] [varchar](200) NULL,	
	[resumeLink] [varchar](MAX)  NULL,
	[comfirmationStatus] [varchar](100) NOT NULL,	
	[zoomLink] [varchar](MAX) NOT NULL,	
	[zoomDetails] [varchar](MAX)  NULL,
	[risebirdHR] [varchar](200) null,
	[interviewer] [varchar](200) null,
	[interviewerComment] [varchar](MAX) null,	
	[intervieweStatus] [varchar](100) null,
	[selectionStatus] [varchar](100) null,
	[videoLink] [varchar](MAX) null,
	[reportLink] [varchar](MAX) null,
	[trainableComment] [varchar](MAX) null,
	[IntervIewOnDevice] [varchar](500) null,
	[hrCoordinatorComment] [varchar](MAX) null,
	[fraudSyncComment] [varchar](MAX) null,
	[sCreatedBy] [varchar](100) NOT NULL CONSTRAINT [DF_InterviewMeetings_sCreatedBy]  DEFAULT ('NA'),	
	[dtCreatedOn] [varchar] (100)NOT NULL CONSTRAINT [DF_InterviewMeetings_dtCreatedOn] DEFAULT ('NA'),	
	[sModifiedBy] [varchar](100) NOT NULL CONSTRAINT [DF_InterviewMeetings_sModifiedBy]  DEFAULT ('NA'),	
	[dtModifiedOn] [varchar](100) NOT NULL CONSTRAINT [DF_InterviewMeetings_dtModifiedOn]  DEFAULT ('NA'),	
 CONSTRAINT [PK_InterviewMeetings] PRIMARY KEY CLUSTERED 		
(		
	[id] ASC	
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)		
)		

END
GO

