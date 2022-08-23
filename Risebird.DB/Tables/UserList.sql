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
			[UserPassword] [nvarchar](60) NULL,
			[WhatsappNumber] [varchar](15) NULL,
			[PasswordCreation] [Datetime] NULL,
            [uniqueid] [varchar](50) NULL,
            [AadhaarVerified] [bit] NULL,	
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
    ALTER TABLE UserList
    Add [Isonline] [bit] NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'UserPassword')
      AND Object_ID = Object_ID(N'UserList'))
BEGIN
    ALTER TABLE UserList
    Add [UserPassword] [nvarchar](60) NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'WhatsappNumber')
      AND Object_ID = Object_ID(N'UserList'))
BEGIN
    ALTER TABLE UserList
    Add [WhatsappNumber] [varchar](15) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PasswordCreation')
      AND Object_ID = Object_ID(N'UserList'))
BEGIN
    ALTER TABLE UserList
    Add [PasswordCreation] [datetime] NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'uniqueid')
      AND Object_ID = Object_ID(N'UserList'))
BEGIN
    ALTER TABLE UserList
    Add [uniqueid] [varchar](50) NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AadhaarVerified')
      AND Object_ID = Object_ID(N'UserList'))
BEGIN
    ALTER TABLE UserList
    Add [AadhaarVerified] [bit] NULL
END
END

GO
