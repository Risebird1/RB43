


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchInstaZoomAccounts'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInstaZoomAccounts
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchInstaZoomAccounts]
    @id INT
AS
BEGIN
   SELECT * FROM InstaZoomMeetingAccounts
END
GO
