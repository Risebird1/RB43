/****** Object:  StoredProcedure [dbo].[usp_UpdateUploadedVideoLinkForInstaInterview]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateUploadedVideoLinkForInstaInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateUploadedVideoLinkForInstaInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_UpdateUploadedVideoLinkForInstaInterview]  
@interviewId int,
@videoLink varchar(1000)
AS  
Begin 

update InstaInterviewMeetings set VideoLink=@videoLink where id=@interviewId

End
GO


