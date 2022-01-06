

IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_GetInterviewerPrimarySkillsCommaSeparated]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_GetInterviewerPrimarySkillsCommaSeparated
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function [dbo].[fn_GetInterviewerPrimarySkillsCommaSeparated](@interviewerId int)
Returns varchar(max)
as
Begin

DECLARE @tbl_PrimarySkill TABLE (    
    Skill varchar(500)     
); 

insert into @tbl_PrimarySkill (Skill) 
select sk.[Name]  from 
TransInterviewerSkill tis
join Skill sk on tis.SkillId = sk.id
where tis.InterviewerProfileId =@interviewerId


declare @tmp varchar(max)
SET @tmp = ''
select @tmp = @tmp + Skill + ', ' from @tbl_PrimarySkill
set @tmp =	CASE @tmp WHEN null THEN null 
			ELSE (
				CASE LEN(@tmp) WHEN 0 THEN @tmp 
				ELSE LEFT(@tmp, LEN(@tmp) - 1) 
				END 
			) END
return @tmp

End
GO



