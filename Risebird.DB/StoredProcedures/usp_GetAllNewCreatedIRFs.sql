IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllNewCreatedIRFs'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllNewCreatedIRFs
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetAllNewCreatedIRFs]   
@timeNow datetime
AS    
Begin        
Select * from IRF where 15 > DATEDIFF(Minute, Createdon, @timeNow)     
End
Go