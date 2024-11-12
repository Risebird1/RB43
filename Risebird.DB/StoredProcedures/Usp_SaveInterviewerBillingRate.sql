IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_SaveInterviewerBillingRate'))
BEGIN
    DROP PROCEDURE dbo.Usp_SaveInterviewerBillingRate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure Usp_SaveInterviewerBillingRate
@IRFid int,
@Userid int,
@billingrate int,
@billingtype varchar(10),
@CreatedOn datetime=null
AS
BEGIN
if exists(Select top 1 id from InterviewerBillingRate where IRFid=@IRFid and Userid=@Userid)
begin
Update InterviewerBillingRate set BillingRate=@billingrate,BillingType=@billingtype where IRFid=@IRFid and Userid=@Userid
end
else
begin
Insert into InterviewerBillingRate (IRFId,Userid,BillingRate,BillingType) values(@IRFid,@Userid,@billingrate,@billingtype)
end
END
GO