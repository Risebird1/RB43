 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertInstaZoomAccount'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertInstaZoomAccount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
 CREATE PROCEDURE [dbo].[usp_InsertInstaZoomAccount]     
@AccountEmail varchar(150)   
AS    
Begin    
Insert into InstaZoomMeetingAccounts (AccountEmailid,Active) values(@AccountEmail,1)
End    
GO
