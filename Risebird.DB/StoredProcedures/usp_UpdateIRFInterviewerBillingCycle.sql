IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateIRFInterviewerBillingCycle'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateIRFInterviewerBillingCycle
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateIRFInterviewerBillingCycle
@irfId int,
@billingCycle int,
@emailid varchar(150)
AS
BEGIN
Declare @Userid int
Select @Userid=ISNull(id,0) From UserList where EmailId=@emailid
Insert into IRFActivityStatus(Userid,IRFId,Comment,Status) values(@Userid,@irfId,@billingCycle,'BillingCycleChanged')

Update IRF set InterviewerBillingCycle=@billingCycle where id=@irfId
END
GO