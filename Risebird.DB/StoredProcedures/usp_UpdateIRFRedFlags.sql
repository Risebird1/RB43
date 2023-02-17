IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateIRFRedFlags'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateIRFRedFlags
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateIRFRedFlags  
@timeNow datetime  
As  
BEGIN  
Update IRF set flagcount=dbo.fn_FalgCountForIrfStats(id,DateAdd(minute,330,Getdate()))  
END  
GO