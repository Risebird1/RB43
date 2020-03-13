/****** Object:  Table [dbo].[Support]    Script Date: 03-03-2020 6.33.01 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Support')
BEGIN
CREATE TABLE [dbo].[Support](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NULL,
	[EmailId] [nvarchar](200) NULL,
	[MobileNumber] [nvarchar](200) NULL,
	[WhatsappNumber] [nvarchar](200) NULL,
	[OnVoiceCall] [bit] NOT NULL DEFAULT 0,
	[OnWhatsapp] [bit] NOT NULL DEFAULT 0,
	[OnEmail] [bit] NOT NULL DEFAULT 0,
	[AvailabilityHours] [nvarchar](max) NULL,
 CONSTRAINT [PK_Support] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END
Go

