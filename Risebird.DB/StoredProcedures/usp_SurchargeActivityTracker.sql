IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SurchargeActivityTracker'))
BEGIN
    DROP PROCEDURE dbo.usp_SurchargeActivityTracker
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SurchargeActivityTracker]     
@InterviewId int,
@IRFId int,
@UserId int,
@Type varchar(50)
As
Begin
If(@Type='Interview')
Begin
Select sat.Surcharge,sat.SurchargeComment,sat.RequestBy,sat.RequestAt,sat.RequestType,im.interviewer,im.IRFId as SkillChannel,'Interview' as Type
from SurchargeActivityTracker sat Join InterviewMeetings im on sat.InterviewId=im.id where sat.InterviewId=@InterviewId

End
Else
Begin
Select sat.Surcharge,sat.SurchargeComment,sat.RequestBy,sat.RequestAt,sat.RequestType,ul.EmailId as interviewer,IRF.IRFId as SkillChannel,'Skill' as Type
from SurchargeActivityTracker sat Join TransInterviewer ti on sat.IRFId=ti.IRFId and sat.UserId=ti.UserId
Join UserList ul on ti.UserId=ul.id
join IRF on ti.IRFId = IRF.id  where sat.IRFId=@IRFId and sat.UserId=@UserId
End
End 
GO