IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDeliveryHeadSampleQuestionById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDeliveryHeadSampleQuestionById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetDeliveryHeadSampleQuestionById]
@id int

AS
Begin
		
	Select * from SkillChannelSampleQuestions  where id = @id

END

GO


