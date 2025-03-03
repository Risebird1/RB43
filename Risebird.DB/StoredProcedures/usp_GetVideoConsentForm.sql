IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetVideoConsentForm'))
BEGIN
    DROP PROCEDURE dbo.usp_GetVideoConsentForm
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetVideoConsentForm    
@irfid int    
AS    
BEGIN    
Select CandidateVideo From IRF where id=@irfid    
END 
GO