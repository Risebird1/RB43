

IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_GetClientActionForInterviewer]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_GetClientActionForInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function [dbo].[fn_GetClientActionForInterviewer](@interviewId int)
Returns varchar(max)
as
Begin

declare @interviewerUserId int
declare @irfId int
set @interviewerUserId = (select ul.id from UserList ul join InterviewMeetings im on im.interviewer = ul.EmailId where im.id = @InterviewId)
Select @irfId=IRF.id from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where im.id=@Interviewid

declare @result varchar(25)

set @result = (Select distinct ISNULL(ti.ClientAction,tie.ClientAction) as ClientAction From  InterviewMeetings im join IRF on IRF.IRFId=im.IRFId 
				join UserList ul on ul.EmailId=im.interviewer 
				left join TransInterviewer ti on ti.UserId=ul.id and ti.IRFId=irf.id 
				left join TransEmergencyInterviewer tie on tie.UserId=ul.id 
				and ti.IRFId=irf.id where ul.id=@interviewerUserId and im.id=@interviewId)

return @result

End
GO



