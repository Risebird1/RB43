IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchIRFInterviewerBillingCycle'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchIRFInterviewerBillingCycle
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchIRFInterviewerBillingCycle
@irfId int
AS
BEGIN
Select ISNULL(InterviewerBillingCycle,0) From IRF where id=@irfId
END
Go