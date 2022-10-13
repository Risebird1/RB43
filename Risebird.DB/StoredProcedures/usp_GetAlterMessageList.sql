IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAlterMessageList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAlterMessageList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAlterMessageList]                    
 AS                            
BEGIN                
select * from alertmessage   
END     
GO 