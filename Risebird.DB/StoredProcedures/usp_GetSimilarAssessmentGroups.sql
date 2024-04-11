IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSimilarAssessmentGroups'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSimilarAssessmentGroups
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetSimilarAssessmentGroups
@searchValue varchar(1000),
@isAlreadyMasterAG bit,
@forEditMAG bit = null
as
BEGIN

declare @tbl_SplitParts table (matchVal varchar(50))


declare @tbl_Result table (id int, AssessmentGroup varchar(100), isDirectlyTargeted bit)

insert into @tbl_SplitParts
SELECT value
FROM STRING_SPLIT(@searchValue, ' ');


while exists (select top 1 matchVal from @tbl_SplitParts)
Begin

declare @searchForSubstring varchar(50)
set @searchForSubstring = trim((select top 1 matchVal from @tbl_SplitParts))


IF @searchForSubstring != ''
Begin
insert into @tbl_Result
select id, AssessmentGroup, 1 from AssessmentGroup where AssessmentGroup like '%' + @searchForSubstring + '%'

End

delete from @tbl_SplitParts where matchVal = @searchForSubstring
End

insert into @tbl_Result
select id, AssessmentGroup, 0 from AssessmentGroup where id not in (select id from @tbl_Result)


if(@isAlreadyMasterAG =1)
Begin
	if(@forEditMAG is not null and @forEditMAG = 1)
	Begin
		select distinct id, AssessmentGroup, isDirectlyTargeted from @tbl_Result 
		where id not in (select AssessmentGroupId from SecondaryAssessmentGroups)

	end 
	Else
	Begin
		select distinct id, AssessmentGroup, isDirectlyTargeted from @tbl_Result 
	End

End
else
Begin
	select distinct id, AssessmentGroup, isDirectlyTargeted from @tbl_Result where id not in (select AssessmentGroupId from SecondaryAssessmentGroups)

End

END
Go
