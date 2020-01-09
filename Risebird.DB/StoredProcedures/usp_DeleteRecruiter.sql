/****** Object:  StoredProcedure [dbo].[usp_DeleteRecruiter]    Script Date: 08-01-2020 6.44.58 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteRecruiter'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteRecruiter
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_DeleteRecruiter]
@userId int

AS
Begin
declare
@id int = (select id from Recruiter where UserId=@userId)

delete from Recruiter where id =@id
delete from UserList where id =@userId

END

GO


