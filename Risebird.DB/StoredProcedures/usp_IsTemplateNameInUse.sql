  
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_IsTemplateNameInUse'))
BEGIN
    DROP PROCEDURE [dbo].[usp_IsTemplateNameInUse]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_IsTemplateNameInUse]  
  
@templateName varchar(100)  
  
AS  
Begin  
	declare @IsUsed bit
	set @IsUsed=0

	if exists (select id from SkillChannelTemplate where TemplateName = @templateName)
	begin
		set @IsUsed=1
	end

	select @IsUsed
 
End  
  
  