IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'StackImages')
BEGIN
    CREATE TABLE [dbo].[StackImages](
        [id]             [int] IDENTITY(1,1) NOT NULL,
        [Code]           [varchar](50)  NOT NULL,
        [DisplayName]    [varchar](200) NOT NULL,
        [Toolchain]      [varchar](500) NOT NULL,
        [GalleryImageId] [varchar](500) NOT NULL,
        [OsType]         [varchar](25)  NOT NULL,
        [IsActive]       [bit]          NOT NULL CONSTRAINT DF_StackImages_IsActive DEFAULT 1,
     CONSTRAINT [PK_StackImages] PRIMARY KEY CLUSTERED ([id] ASC),
     CONSTRAINT [UQ_StackImages_Code] UNIQUE ([Code])
    ) ON [PRIMARY];
END
GO
