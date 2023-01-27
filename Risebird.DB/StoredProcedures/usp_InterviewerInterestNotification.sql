IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InterviewerInterestNotification'))
BEGIN
    DROP PROCEDURE dbo.usp_InterviewerInterestNotification
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_InterviewerInterestNotification]       
AS      
BEGIN      
select ip.id, ul.FirstName,ul.LastName,ul.EmailId,ul.id as UserId,ul.uniqueid 
From InterviewerProfile ip inner join [dbo].[UserList] ul on ip.UserId = ul.id  
where isInterest is null
END
GO