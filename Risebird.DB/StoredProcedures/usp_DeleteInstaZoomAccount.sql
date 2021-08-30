IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteInstaZoomAccount'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteInstaZoomAccount
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_DeleteInstaZoomAccount]     
@id int    
AS    
Begin    
Delete from InstaZoomMeetingDetails where Accountid=@id    
Delete from InstaZoomMeetingAccounts where id=@id
End    
  GO 