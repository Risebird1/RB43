/****** Object:  StoredProcedure [dbo].[usp_GetTransQualityController]    Script Date: 12-11-2019 5.50.16 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransQualityController'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransQualityController
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTransQualityController]

@irfId int

AS
Begin

select TransQualityController.*, Interviewer.PrimarySkill from TransQualityController 
inner join 
Interviewer on TransQualityController.InterviewerId = Interviewer.id where IRFId=@irfId

End

GO


