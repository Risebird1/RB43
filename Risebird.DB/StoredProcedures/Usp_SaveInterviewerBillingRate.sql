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
@billingtype varchar(10) = NULL,  -- if NULL, resolved from Client.BillingCurrency via IRF
@CreatedOn datetime = null
AS
BEGIN
    Declare @resolvedBillingType varchar(10)

    -- Resolve currency from Client table when not explicitly supplied
    Select @resolvedBillingType = ISNULL(@billingtype, ISNULL(cl.BillingCurrency, 'INR'))
    From IRF
    Join Client cl ON cl.id = IRF.ClientId
    Where IRF.id = @IRFid

    Set @resolvedBillingType = ISNULL(@resolvedBillingType, 'INR')

    if exists(Select top 1 id from InterviewerBillingRate where IRFid=@IRFid and Userid=@Userid)
    begin
        Update InterviewerBillingRate set BillingRate=@billingrate, BillingType=@resolvedBillingType where IRFid=@IRFid and Userid=@Userid
    end
    else
    begin
        Insert into InterviewerBillingRate (IRFId,Userid,BillingRate,BillingType) values(@IRFid,@Userid,@billingrate,@resolvedBillingType)
    end
END
GO