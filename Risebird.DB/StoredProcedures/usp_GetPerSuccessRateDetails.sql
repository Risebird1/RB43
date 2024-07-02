IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetPerSuccessRateDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPerSuccessRateDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetPerSuccessRateDetails]     
@InterviewId int,
@UserId int,
@IRFId int,
@Type varchar(50)
As
Begin
if(@Type='Interview')
Begin
Select IRF.Heuristicsscore,ti.PerSuccessRate from InterviewMeetings im 
join IRF on im.IRFId = IRF.IRFId
join UserList uli on im.interviewer= uli.EmailId       
join TransInterviewer ti on ti.Userid=uli.id and ti.IRFId=IRF.id  where im.id=@InterviewId
End
End  
GO