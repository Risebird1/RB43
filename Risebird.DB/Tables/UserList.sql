IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'UserList')
BEGIN  
CREATE TABLE [dbo].[UserList](					
			[id] [int] IDENTITY(1,1) NOT NULL,		
			[idProfile] [int] NOT NULL,		
			[FirstName] [varchar](150) NULL,		
			[LastName] [varchar](150) NULL,		
			[EmailId] [varchar](150) NOT NULL,
			[MobileNumber][BIGINT] NULL,	
			[LastLogin] [datetime] NULL,		
			[LastLogout] [datetime] NULL,		
			[IsActive] [bit] NULL,			
			[ImageURL] [nvarchar](1000) NULL,
			[Isonline] [bit] NULL,	
			CONSTRAINT [FK_User_Group] FOREIGN KEY (idProfile) REFERENCES Profile(id),
 CONSTRAINT [PK_UserList] PRIMARY KEY CLUSTERED 					
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
    WHERE Name IN (N'Isonline')
      AND Object_ID = Object_ID(N'UserList'))
BEGIN
    ALTER TABLE Client
    Add [Isonline] [bit] NULL
END
END

GO
