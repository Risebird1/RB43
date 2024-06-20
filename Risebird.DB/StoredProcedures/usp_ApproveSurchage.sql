IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ApproveSurchage'))
BEGIN
    DROP PROCEDURE dbo.usp_ApproveSurchage
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_ApproveSurchage]     
@InterviewId int,
@Type varchar(50),
@Surcharge int,
@SurchargeComment varchar(500),
@RequestBy varchar(150),
@RequestAt datetime,
@RequestType Varchar(50),
@actionType int,
@IRFId int
As
Begin
Declare @InterviewIdType int;
If(@actionType=1) --Approve
Begin
If(@Type='Interview')
Begin
update  InterviewMeetings set IsSurchargeApprove=1 where id=@InterviewId
set @InterviewIdType=@InterviewId;
End
Else
Begin
update  TransInterviewer set IsSurchargeApprove=1 where IRFId=@IRFId
set @InterviewIdType=0;
End
Insert Into SurchargeActivityTracker(InterviewId,Surcharge,SurchargeComment,RequestBy,RequestAt,RequestType,IRFId) values(@InterviewIdType,@Surcharge,@SurchargeComment,@RequestBy,@RequestAt,@RequestType,@IRFId)
End
Else
Begin
If(@Type='Interview')
Begin
update  InterviewMeetings set IsSurchargeApprove=0,Surcharge=0,SurchargeComment=null where id=@InterviewId
set @InterviewIdType=@InterviewId;
End
Else
Begin
update  TransInterviewer set IsSurchargeApprove=0 ,Surcharge=0  where IRFId=@IRFId
set @InterviewIdType=0;
End
Insert Into SurchargeActivityTracker(InterviewId,Surcharge,SurchargeComment,RequestBy,RequestAt,RequestType,IRFId) values(@InterviewIdType,0,null,@RequestBy,@RequestAt,@RequestType,@IRFId)
End
End 
GO