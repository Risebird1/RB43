IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerRedMarker')
BEGIN  
CREATE TABLE [dbo].[TransInterviewerRedMarker](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[IRFId] [int] NOT NULL,
			[InterviewerId] [int] NOT NULL,	
            [RedMarker] [int] NULL,
            [RedMarkerStatus] [int] NULL,
            [NegativeAIVerdictStatus] [int] NULL,
			CONSTRAINT [FK_TIRIRFId] FOREIGN KEY (IRFId) REFERENCES IRF(id),
			CONSTRAINT [FK_TIR_InterviewerId] FOREIGN KEY (InterviewerId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerRedMarker] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					
END
Else
begin
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'RedMarkerStatus')
      AND Object_ID = Object_ID(N'TransInterviewerRedMarker'))
BEGIN
    ALTER TABLE TransInterviewerRedMarker
    Add RedMarkerStatus INT
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'NegativeAIVerdictStatus')
      AND Object_ID = Object_ID(N'TransInterviewerRedMarker'))
BEGIN
    ALTER TABLE TransInterviewerRedMarker
    Add NegativeAIVerdictStatus INT
END
end
GO
