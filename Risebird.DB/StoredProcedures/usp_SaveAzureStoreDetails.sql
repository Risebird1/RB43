IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveAzureStoreDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveAzureStoreDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveAzureStoreDetails]     
@Container varchar(200),
@MemorySpace int,
@CreatedAt datetime
as
begin
Insert Into AzureStorage(Container,MemorySpace,CreatedAt) values(@Container,@MemorySpace,@CreatedAt)
End  
GO