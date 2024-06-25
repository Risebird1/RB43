IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateSurchargeDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateSurchargeDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_UpdateSurchargeDetails]     
@InterviewId int,
@IRFId int,
@Type varchar(50),
@UserId int,
@SurchargeComment varchar(500),
@Surchargeamt int,
@RequestBy varchar(150),
@RequestAt Datetime
As
Begin
declare @NewInterviewId int
declare @NewUserId int
If(@Type='Interview')
Begin
Update InterviewMeetings set Surcharge=@Surchargeamt,SurchargeComment=@SurchargeComment,IsSurchargeApprove=null where Id=@InterviewId
set @NewInterviewId=@InterviewId
set @NewUserId=null
End
Else
Begin
Update TransInterviewer set Surcharge=@Surchargeamt,IsSurchargeApprove=null where IRFId=@IRFId and UserId=@UserId
set @NewInterviewId=null
set @NewUserId=@UserId
End
Insert Into SurchargeActivityTracker(InterviewId,Surcharge,SurchargeComment,RequestBy,RequestAt,RequestType,IRFId,UserId)
values(@NewInterviewId,@Surchargeamt,@SurchargeComment,@RequestBy,@RequestAt,'Modify',@IRFId,@NewUserId)
End
GO