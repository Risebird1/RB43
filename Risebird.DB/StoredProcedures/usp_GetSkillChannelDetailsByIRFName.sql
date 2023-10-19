IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillChannelDetailsByIRFName'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetSkillChannelDetailsByIRFName]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetSkillChannelDetailsByIRFName]               
@irfName varchar(100)    
AS                
BEGIN 
      
select id, PrimarySkillInvite, FlatRate from IRF where IRFId  = @irfName

END


GO

    
      