/****** Object:  Table [dbo].[ActivityTracker]    Script Date: 13-01-2021 5.14.41 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ActivityTracker')
BEGIN 

CREATE TABLE [dbo].[ActivityTracker](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ActivityType] [int] NOT NULL,
	[PerformedBy] [varchar](100) NOT NULL,
	[Summary] [varchar](max) NULL,
	[FileUrl] [varchar](max) NULL,
	[PerformedAt] [datetime] NOT NULL,
	[Particulars] [varchar](150) NULL,
	[ResourceUrl] [varchar](500) NULL,
 CONSTRAINT [PK_ActivityTracker] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

End


Else

Begin
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ResourceUrl')
      AND Object_ID = Object_ID(N'ActivityTracker'))
BEGIN
    ALTER TABLE ActivityTracker
    Add [ResourceUrl] varchar(500)
END
End

GO

sp_RENAME 'ActivityTracker.[Name]' , 'Particulars', 'COLUMN'
