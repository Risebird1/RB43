/****** Object:  StoredProcedure [dbo].[usp_AddTransInterviewer]    Script Date: 10-01-2020 12.57.14 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddTransRecruiter'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTransRecruiter
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddTransRecruiter]

@IRFId int,
@UserId int

AS
Begin

insert into [dbo].[TransRecruiter] values (@IRFId,(select id from [dbo].[Recruiter] where UserId = @UserId), @UserId)

End
GO


