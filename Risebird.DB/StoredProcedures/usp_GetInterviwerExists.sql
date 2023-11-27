IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviwerExists'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviwerExists
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetInterviwerExists
@interviewer varchar(100)
As
Begin
select Upper(EmailId) from [dbo].[UserList] where EmailId= @interviewer
END
GO