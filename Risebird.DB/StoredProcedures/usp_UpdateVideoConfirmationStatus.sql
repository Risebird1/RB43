IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateVideoConfirmationStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateVideoConfirmationStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateVideoConfirmationStatus]   
@Emailid varchar(150),
@status int
AS  
BEGIN  
Update InterviewerProfile set VideoConfirmation=@status where Userid in(Select id From UserList where Emailid=@Emailid)
END  
GO 




