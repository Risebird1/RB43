/****** Object:  Table [dbo].[TransQualityController]    Script Date: 12-11-2019 5.57.29 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransQualityController')
BEGIN  
CREATE TABLE [dbo].[TransQualityController](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[InterviewerId] [int] NOT NULL,
	[UserId] [int] NULL,
    [IsActive] [bit] NOT NULL DEFAULT 1,
	CONSTRAINT [FK__TransQualityController] FOREIGN KEY (IRFId) REFERENCES IRF(id),
	CONSTRAINT [FK__TransQualityController_1] FOREIGN KEY (InterviewerId) REFERENCES Interviewer(id),
 CONSTRAINT [PK_TransQualityController] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerSuccessRate')
      AND Object_ID = Object_ID(N'TransQualityController'))
BEGIN
    ALTER TABLE TransQualityController
    Add PerSuccessRate INT
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsActive')
      AND Object_ID = Object_ID(N'TransQualityController'))
BEGIN
    ALTER TABLE TransQualityController
    Add IsActive bit NOT NULL default 1
END
end

GO









