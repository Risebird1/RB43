/****** Object:  StoredProcedure [dbo].[usp_AddTransResumeEvaluator]    Script Date: 09-12-2019 7.48.10 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddTransResumeEvaluator'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTransResumeEvaluator
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_AddTransResumeEvaluator]

@irfId int,
@ResumeEvaluatorUserId int

AS
Begin

insert into [dbo].[TransResumeEvaluator] values (@irfId,(select id from [dbo].[ResumeEvaluator] where UserId=@ResumeEvaluatorUserId) )

End


GO


