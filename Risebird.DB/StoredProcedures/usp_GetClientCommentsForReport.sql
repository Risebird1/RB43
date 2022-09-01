/****** Object:  StoredProcedure [dbo].[usp_GetClientCommentsForReport]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('[usp_GetClientCommentsForReport]'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientCommentsForReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_GetClientCommentsForReport]  
  
@interviewId int  
  
AS  
Begin  

select * from ClientCommentsForReport where InterviewId = @interviewId

End  
  
  
GO


