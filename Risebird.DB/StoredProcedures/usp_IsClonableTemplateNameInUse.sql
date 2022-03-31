  
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_IsClonableTemplateNameInUse'))
BEGIN
    DROP PROCEDURE [dbo].[usp_IsClonableTemplateNameInUse]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_IsClonableTemplateNameInUse]  
  
@name varchar(100)  
  
AS  
Begin  
	declare @IsUsed bit
	set @IsUsed=0

	if exists (select id from ClonableSkillChannel where [Name] = @name)
	begin
		set @IsUsed=1
	end

	select @IsUsed
 
End  
  
  