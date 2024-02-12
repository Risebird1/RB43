IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSimilarAssessmentGroups'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSimilarAssessmentGroups
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetSimilarAssessmentGroups
@searchValue varchar(1000)
as
BEGIN

declare @tbl_SplitParts table (matchVal varchar(50))


declare @tbl_Result table (id int, AssessmentGroup varchar(100))

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
select id, AssessmentGroup from AssessmentGroup where AssessmentGroup like '%' + @searchForSubstring + '%'

End

delete from @tbl_SplitParts where matchVal = @searchForSubstring
End


select distinct id, AssessmentGroup from @tbl_Result

END
Go


