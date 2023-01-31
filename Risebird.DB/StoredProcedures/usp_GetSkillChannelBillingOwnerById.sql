IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSkillChannelBillingOwnerById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelBillingOwnerById
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetSkillChannelBillingOwnerById
@IRFId int
AS
BEGIN
Select ul.EmailId,ul.uniqueid,IRF.IRFId,ul.FirstName as Name From IRF join UserList ul on IRF.BillingOwner=ul.id 
where IRF.id=@IRFId
END
Go