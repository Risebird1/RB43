/****** Object:  StoredProcedure [dbo].[usp_AddTransHR]    Script Date: 09-12-2019 7.47.42 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddTransHR'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTransHR
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddTransHR]

@irfId int,
@HRUserId int

AS
Begin

insert into [dbo].[TransHR] values (@irfId,(select id from [dbo].[HR] where UserId=@HRUserId) )

End

GO


