IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDeliveryHeadSampleQuestions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDeliveryHeadSampleQuestions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetDeliveryHeadSampleQuestions]
@IRFId int,   
@emailId varchar(100)


AS
Begin
		
if(@emailId is not null)
begin
	declare @profile int
	set @profile = (select idProfile from UserList where EmailId like @emailId)

	if(@profile=1)--Admin
	begin
		Select * from SkillChannelSampleQuestions  where IRFId = @IRFId
	end
	else
	begin 
		Select * from SkillChannelSampleQuestions where IRFId = @IRFId and CreatedBy = @emailId
	end
end
else
begin
	Select * from SkillChannelSampleQuestions  where IRFId = @IRFId
end

END

GO


