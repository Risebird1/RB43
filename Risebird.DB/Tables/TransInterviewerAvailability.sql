IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerAvailability')
BEGIN 


CREATE TABLE [dbo].[TransInterviewerAvailability](
	[Id] [int] IDENTITY(1,1) NOT NULL,  -- PK
	[InterviewerProfileId] [int] NOT NULL, -- FK
	[DayTimeSlot] [varchar] (500) NULL,
	[DaySlot] [varchar] (500) NULL,
	[StartTime] [datetime] NULL,
	[EndTime] [datetime] NULL,
	CONSTRAINT [FK__IProfileID_Availability] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerAvailability] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)

END

GO