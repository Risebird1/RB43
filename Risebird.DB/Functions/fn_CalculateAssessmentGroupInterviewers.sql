
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_CalculateAssessmentGroupInterviewers]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_CalculateAssessmentGroupInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_CalculateAssessmentGroupInterviewers(@assessmentGroupId int)
Returns int
as
Begin

declare @totalCount int
set @totalCount=0

if exists(select top 1 id from MasterAssessmentGroup where AssessmentGroupId = @assessmentGroupId)
Begin

Select @totalCount = count(distinct ipr.id) from InterviewerProfile ipr 
join UserList ul on ul.id = ipr.UserId
cross Apply string_split(AssessmentGroupId,',') as f
where ul.IsActive = 1 and (f.value =@assessmentGroupId or  f.value in (select AssessmentGroupId from SecondaryAssessmentGroups where MAGId = (select id from MasterAssessmentGroup where AssessmentGroupId = @assessmentGroupId)))

End
Else
Begin

Select @totalCount = count(distinct ipr.id) from InterviewerProfile ipr 
join UserList ul on ul.id = ipr.UserId
cross Apply string_split(AssessmentGroupId,',') as f
where ul.IsActive = 1 and f.value =@assessmentGroupId

End

 return @totalCount

End

