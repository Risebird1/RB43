IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAlterMessage'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAlterMessage
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAlterMessage]                
@Currenttime datetime     
AS                  
BEGIN            
select Announcement from alertmessage where Starttime<@Currenttime and EndTime>@Currenttime
END  
GO
  