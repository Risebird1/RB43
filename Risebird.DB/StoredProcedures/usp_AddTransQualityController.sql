/****** Object:  StoredProcedure [dbo].[usp_AddTransQualityController]    Script Date: 12-11-2019 12.03.20 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddTransQualityController'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTransQualityController
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_AddTransQualityController]

@irfIntId int,
@InterviewerUserId int

AS
Begin


insert into [dbo].[TransQualityController] values (@irfIntId,(select id from [dbo].[Interviewer] where userid=@InterviewerUserId), @InterviewerUserId )

End

GO


